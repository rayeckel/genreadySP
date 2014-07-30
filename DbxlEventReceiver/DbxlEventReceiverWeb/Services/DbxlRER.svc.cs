using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.EventReceivers;
using Microsoft.SharePoint.Client.UserProfiles;
using System.Xml;
using System.Xml.XPath;
using System.IO;
using System.Net;
using System.Resources;
using GRSPClassLibrary.Web.Log;
using DBXLClassLibrary;
using DBXLClassLibrary.DbxlDocumentService;
using DBXLEventReceiverWeb.Base;

namespace DBXLEventReceiverWeb.Services
{
    public class DbxlRER : IRemoteEventService
    {
        private LogWriter syslogWriter;
        private LogWriter errorlogWriter;

        /// <summary>
        /// Handles events that occur before an action occurs, such as when a user adds or deletes a list item.
        /// </summary>
        /// <param name="properties">Holds information about the remote event.</param>
        /// <returns>Holds information returned from the remote event.</returns>
        public SPRemoteEventResult ProcessEvent(SPRemoteEventProperties properties)
        {
            ClientContext clientContext = getClientContext(properties);

            if (clientContext != null)
            {
                syslogWriter = new GRSPClassLibrary.Web.Log.LogWriter("System Log", clientContext);
                errorlogWriter = new GRSPClassLibrary.Web.Log.LogWriter("Error Log", clientContext);

                using (clientContext)
                {
                    //logWriter.WriteLog("RER fired", "Process Event : Position 1");

                    clientContext.Load(clientContext.Web);
                    clientContext.ExecuteQuery();

                    Boolean RerEnabled = RERIsEnabled(properties, clientContext);
                    if (RerEnabled)
                    {
                        ExecuteRER(properties, clientContext);
                    }
                }
            }

            SPRemoteEventResult result = new SPRemoteEventResult();
            return result;
        }

        /// <summary>
        /// Handles events that occur after an action occurs, such as after a user adds an item to a list or deletes an item from a list.
        /// </summary>
        /// <param name="properties">Holds information about the remote event.</param>
        public void ProcessOneWayEvent(SPRemoteEventProperties properties)
        {
            ClientContext clientContext = getClientContext(properties);

            if (clientContext != null)
            {
                syslogWriter = new GRSPClassLibrary.Web.Log.LogWriter(Constants.SYSTEM_LOG_LABEL, clientContext);
                errorlogWriter = new GRSPClassLibrary.Web.Log.LogWriter(Constants.ERROR_LOG_LABEL, clientContext);

                using (clientContext)
                {
                    //logWriter.WriteLog("RER fired", "Process Event : Position 1");

                    clientContext.Load(clientContext.Web);
                    clientContext.ExecuteQuery();

                    Boolean RerEnabled = RERIsEnabled(properties, clientContext);
                    if (RerEnabled)
                    {
                        ExecuteRER(properties, clientContext);
                    }
                }
            }
        }

        private ClientContext getClientContext(SPRemoteEventProperties properties)
        {
            string webUrl = properties.ItemEventProperties.WebUrl.ToString();
            var webUri = new Uri(webUrl);

            string realm = TokenHelper.GetRealmFromTargetUrl(webUri);
            string accessToken = TokenHelper.GetAppOnlyAccessToken(TokenHelper.SharePointPrincipal, webUri.Authority, realm).AccessToken;
            ClientContext clientContext = TokenHelper.GetClientContextWithAccessToken(webUrl, accessToken);

            return clientContext;
        }

        private Boolean RERIsEnabled(SPRemoteEventProperties properties, ClientContext clientContext)
        {
            //check and execute if RER is enabled on list
            string DbxlRerEnabledProperty = properties.ItemEventProperties.ListId + Constants.KEY_DBXL_PROPERTY_RER_ENABLED;
            Boolean RerEnabled = Convert.ToBoolean(GRSPClassLibrary.Web.Dbxl.Properties.GetDbxlProperty(DbxlRerEnabledProperty, clientContext));
            return RerEnabled;
        }

        private void ExecuteRER(SPRemoteEventProperties properties, ClientContext clientContext)
        {
            //logWriter.WriteLog("RER fired", "EXECUTING");

            //get Dbxl document type for list
            string DbxlDocTypeProperty = properties.ItemEventProperties.ListId + Constants.KEY_DBXL_PROPERTY_DOCTYPE;
            string DbxlDocType = GRSPClassLibrary.Web.Dbxl.Properties.GetDbxlProperty(DbxlDocTypeProperty, clientContext);
            string DbxlDescriptionText = "Updated: " + DateTime.Now;

            Guid listId = properties.ItemEventProperties.ListId;
            int Id = properties.ItemEventProperties.ListItemId;
            ListItem listItem = ClientContextListItem(clientContext, listId, Id);
            XmlDocument Doc = LoadClientFile(clientContext, listItem);
            IDbxlDocumentService DocService = CredentialDocumentService(clientContext);

            var itemEditorLookupValue = (FieldUserValue)listItem[Constants.LIST_ITEM_EDITOR];
            string itemEditor = itemEditorLookupValue.LookupValue;

            switch (properties.EventType)
            {
                case SPRemoteEventType.ItemAdded:
                    {
                        try
                        {
                            //syslogWriter.WriteLog("RER fired", "Item added");

                            int DbxlId;
                            string RefId;
                            StatusInfo SubmitResult =
                                DocService.SubmitDocument(DbxlDocType, Doc.OuterXml, Id.ToString(), itemEditor, DbxlDescriptionText, Constants.TRUE, out DbxlId, out RefId);

                            //System.Diagnostics.Trace.WriteLine("SUCCESS: " + SubmitResult.Success.ToString());
                            if (SubmitResult.Success)
                            {
                                //System.Diagnostics.Trace.WriteLine("DBXL ID: " + DbxlId.ToString());
                                listItem[Constants.DBXL_ID_LABEL] = DbxlId.ToString();
                                listItem[Constants.LIST_ITEM_EDITOR] = itemEditor;
                                listItem.Update();
                                clientContext.ExecuteQuery();
                            }
                            else if (!SubmitResult.Success)
                            {
                                //System.Diagnostics.Trace.WriteLine("ERROR CODE: " + SubmitResult.Errors[0].Code);
                                //System.Diagnostics.Trace.WriteLine("ERROR DESCRIPTION: " + SubmitResult.Errors[0].Description);
                            }
                        }
                        catch (Exception ex)
                        {
                            //System.Diagnostics.Trace.WriteLine("MESSAGE: " + ex.Message);
                            //System.Diagnostics.Trace.WriteLine("SOURCE: " + ex.Source);listId
                            //System.Diagnostics.Trace.WriteLine("INNER EXCEPTION: " + ex.InnerException);
                            //Diagnostics.WriteLog(clientContext.Web, "RER item adding error", ex.Message);
                            //clientContext.ExecuteQuery();
                        }
                        break;
                    }
                case SPRemoteEventType.ItemUpdated:
                    {
                        try
                        {
                            //syslogWriter.WriteLog("RER fired", "Item updated");
                            //System.Diagnostics.Trace.WriteLine("CALLING DBXL CLIENT: ITEM UDPATED");

                            //add qdabra processing instruction
                            int DbxlId = Convert.ToInt32(listItem[Constants.DBXL_ID_LABEL].ToString());
                            DbxlPiXmlProcessingInstruction(Doc, DbxlId, DbxlDocType);

                            string RefId;
                            StatusInfo SubmitResult =
                                DocService.SubmitDocument(DbxlDocType, Doc.OuterXml, Id.ToString(), itemEditor, DbxlDescriptionText, Constants.TRUE, out DbxlId, out RefId);

                            if (SubmitResult.Success)
                            {
                                //System.Diagnostics.Trace.WriteLine("DBXL ID: " + DbxlId.ToString());
                            }
                            else if (!SubmitResult.Success)
                            {
                                //System.Diagnostics.Trace.WriteLine("ERROR CODE: " + SubmitResult.Errors[0].Code);
                                //System.Diagnostics.Trace.WriteLine("ERROR DESCRIPTION: " + SubmitResult.Errors[0].Description);
                            }

                        }
                        catch (Exception ex)
                        {
                            //System.Diagnostics.Trace.WriteLine("MESSAGE: " + ex.Message);
                            //System.Diagnostics.Trace.WriteLine("SOURCE: " + ex.Source);
                            //System.Diagnostics.Trace.WriteLine("INNER EXCEPTION: " + ex.InnerException);
                        }
                        break;
                    }
                case SPRemoteEventType.ItemDeleting:
                    {
                        try
                        {
                            //GenerationReady.Diagnostics.Log.WriteLog(clientContext.Web, "RER fired", "Item deleting");
                            //System.Diagnostics.Trace.WriteLine("CALLING DBXL CLIENT: ITEM DELETING");

                            int DbxlId = Convert.ToInt32(listItem[Constants.DBXL_ID_LABEL].ToString());
                            StatusInfo info = DocService.RemoveDocument(DbxlId);

                            if (info.Success)
                            {
                                //System.Diagnostics.Trace.WriteLine("RESULT: " + info.Success.ToString());
                            }
                            else if (!info.Success)
                            {
                                //System.Diagnostics.Trace.WriteLine("RESULT: " + info.Success.ToString());
                                //System.Diagnostics.Trace.WriteLine("ERROR CODE: " + info.Errors[0].Code);
                                //System.Diagnostics.Trace.WriteLine("ERROR DESCRIPTION: " + SubmitResult.Errors[0].Description);
                            }
                        }
                        catch (Exception ex)
                        {
                            //System.Diagnostics.Trace.WriteLine("MESSAGE: " + ex.Message);
                            //System.Diagnostics.Trace.WriteLine("SOURCE: " + ex.Source);
                            //System.Diagnostics.Trace.WriteLine("INNER EXCEPTION: " + ex.InnerException);
                        }
                        break;
                    }
                default:
                    {
                        errorlogWriter.WriteLog("DBXL Remote Event Receiver ERROR", "No Remote Event Type Provided.");
                        break;
                    }
            }
        }

        private IDbxlDocumentService CredentialDocumentService(ClientContext clientContext)
        {
            string serviceUrl = BuildServiceUrl();
            NetworkCredential credentials = BuildServiceCredentials(clientContext);

            var DocService = new IDbxlDocumentService()
            {
                Url = serviceUrl,
                UseDefaultCredentials = false,
                Credentials = credentials,
                Timeout = 60000
            };

            //string DbxlRootUrl = DocService.Url;
            System.Diagnostics.Trace.WriteLine("DBXL ROOT URL: " + credentials.Password);

            return DocService;
        }

        private string BuildServiceUrl()
        {
            string serviceUrl = DBXLClassLibrary.Properties.Settings.Default.GRSP_DBXL_ServiceUrl;

            if (!serviceUrl.EndsWith("/"))
            {
                serviceUrl = serviceUrl + "/";
            }

            serviceUrl = serviceUrl + Constants.DBXL_DOC_SERVICE_PAGE;

            return serviceUrl;
        }

        private NetworkCredential BuildServiceCredentials(ClientContext clientContext)
        {
            string username = GRSPClassLibrary.Web.Dbxl.Properties.GetDbxlProperty(Constants.DBXL_USERNAME, clientContext);
            string encryptedPassword = GRSPClassLibrary.Web.Dbxl.Properties.GetDbxlProperty(Constants.DBXL_PASSWORD, clientContext);
            //string decryptedPassword = GRSPClassLibrary.Web.Crypt.Decrypt(encryptedPassword);

            var credentials = new NetworkCredential(username, encryptedPassword);
            //var credentials = new NetworkCredential("db001az\johnnie.margerison", "sdW&*fnIdf32");

            return credentials;
        }

        private ListItem ClientContextListItem(ClientContext clientContext, Guid ListId, int Id)
        {
            //System.Diagnostics.Trace.WriteLine(Id.ToString());

            Web Web = clientContext.Web;
            List List = Web.Lists.GetById(ListId);
            ListItem listItem = List.GetItemById(Id);

            //we need to load the listItem, will need to load and execute for the file also
            clientContext.Load(listItem);
            clientContext.ExecuteQuery();

            //System.Diagnostics.Trace.WriteLine("DBXL ID: " + listItem["DbxlId"].ToString());

            return listItem;
        }

        private XmlDocument LoadClientFile(ClientContext clientContext, ListItem listItem)
        {
            Microsoft.SharePoint.Client.File File = listItem.File;
            clientContext.Load(File);
            clientContext.ExecuteQuery();

            //System.Diagnostics.Trace.WriteLine("FILE ID: " + listItem.Id.ToString());
            //System.Diagnostics.Trace.WriteLine("FILE LENGTH: " + File.Length.ToString());

            ClientResult<System.IO.Stream> Stream = File.OpenBinaryStream();
            clientContext.ExecuteQuery();

            var text_reader = new XmlTextReader(Stream.Value);
            var Doc = new XmlDocument();
            Doc.Load(text_reader);

            return Doc;
        }

        private void DbxlPiXmlProcessingInstruction(XmlDocument Doc, int DbxlId, string DbxlDocType)
        {
            String PiText = String.Format("docid=\"{0}\" doctype=\"{1}\"", DbxlId, DbxlDocType);
            XmlProcessingInstruction DbxlPi = Doc.CreateProcessingInstruction(Constants.DBXL_PROCESSING_INSTRUCTION_NAME, PiText);
            Doc.AppendChild(DbxlPi);

            //System.Diagnostics.Trace.WriteLine(String.Format("PROCESSING INSTRUCTION: {0}, {1}", DbxlPi.Target, DbxlPi.Data));
            //System.Diagnostics.Trace.WriteLine(Doc.OuterXml);
        }
    }
}
