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
using GRSPClassLibrary.Web;
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
            ClientContext clientContext = GetClientContext(properties);

            if (clientContext != null)
            {
                BuildLoggingContext(properties);

                using (clientContext)
                {
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
            ClientContext clientContext = GetClientContext(properties);

            if (clientContext != null)
            {
                BuildLoggingContext(properties);

                using (clientContext)
                {
                    Boolean RerEnabled = RERIsEnabled(properties, clientContext);
                    if (RerEnabled)
                    {
                        ExecuteRER(properties, clientContext);
                    }
                }
            }
        }

        private ClientContext GetClientContext(SPRemoteEventProperties properties)
        {
            string webUrl = properties.ItemEventProperties.WebUrl.ToString();
            var sharepointUrl = new Uri(webUrl);

            string appOnlyAccessToken = TokenHelper.GetAccessTokenFromAppOnlyRequest(sharepointUrl);
            ClientContext clientContext = TokenHelper.GetClientContextWithAccessToken(webUrl, appOnlyAccessToken);

            return clientContext;
        }

        private void BuildLoggingContext(SPRemoteEventProperties properties)
        {
            ClientContext loggingContext = GetClientContext(properties);

            syslogWriter = new GRSPClassLibrary.Web.Log.LogWriter(Constants.SYSTEM_LOG_LABEL, loggingContext);
            errorlogWriter = new GRSPClassLibrary.Web.Log.LogWriter(Constants.ERROR_LOG_LABEL, loggingContext);
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
            clientContext.Load(clientContext.Web, web => web.Lists);
            clientContext.ExecuteQuery();

            //get Dbxl document type for list
            string DbxlDocTypeProperty = properties.ItemEventProperties.ListId + Constants.KEY_DBXL_PROPERTY_DOCTYPE;
            string DbxlDocType = GRSPClassLibrary.Web.Dbxl.Properties.GetDbxlProperty(DbxlDocTypeProperty, clientContext);

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
                            int DbxlId;
                            string RefId;
                            string DbxlDescriptionText = "Item Added: " + DateTime.Now;
                            StatusInfo SubmitResult =
                                DocService.SubmitDocument(DbxlDocType, Doc.OuterXml, Id.ToString(), itemEditor, DbxlDescriptionText, Constants.TRUE, out DbxlId, out RefId);

                            if (SubmitResult.Success)
                            {
                                syslogWriter.WriteLog("DBXL RER Triggered", "Item added");

                                listItem[Constants.DBXL_ID_LABEL] = DbxlId.ToString();
                                listItem.Update();
                                clientContext.ExecuteQuery();
                            }
                            else if (!SubmitResult.Success)
                            {
                                errorlogWriter.WriteLog("DBXL RER Item Added ERROR", SubmitResult.Errors[0].Description);
                            }
                        }
                        catch (Exception ex)
                        {
                            errorlogWriter.WriteLog("DBXL RER Item Added ERROR", ex.Message);
                        }
                        break;
                    }
                case SPRemoteEventType.ItemUpdated:
                    {
                        try
                        {
                            //Add qdabra processing instruction
                            int DbxlId = Convert.ToInt32(listItem[Constants.DBXL_ID_LABEL].ToString());
                            DbxlPiXmlProcessingInstruction(Doc, DbxlId, DbxlDocType);

                            string RefId;
                            string DbxlDescriptionText = "Item Updated: " + DateTime.Now;
                            StatusInfo SubmitResult =
                                DocService.SubmitDocument(DbxlDocType, Doc.OuterXml, Id.ToString(), itemEditor, DbxlDescriptionText, Constants.TRUE, out DbxlId, out RefId);

                            if (SubmitResult.Success)
                            {
                                syslogWriter.WriteLog("DBXL RER Triggered", "Item updated");

                                listItem[Constants.DBXL_ID_LABEL] = DbxlId.ToString();
                                listItem[Constants.LIST_ITEM_EDITED_BY] = 13;
                                listItem.Update();
                                clientContext.ExecuteQuery();
                            }
                            else if (!SubmitResult.Success)
                            {
                                errorlogWriter.WriteLog("DBXL RER Item Updated ERROR", SubmitResult.Errors[0].Description);
                            }

                        }
                        catch (Exception ex)
                        {
                            errorlogWriter.WriteLog("DBXL RER Item Updated ERROR", ex.Message);
                        }
                        break;
                    }
                case SPRemoteEventType.ItemDeleting:
                    {
                        try
                        {
                            //Add qdabra processing instruction
                            int DbxlId = Convert.ToInt32(listItem[Constants.DBXL_ID_LABEL].ToString());
                            StatusInfo SubmitResult = DocService.RemoveDocument(DbxlId);

                            if (SubmitResult.Success)
                            {
                                syslogWriter.WriteLog("DBXL RER Triggered", "Item deleted");
                            }
                            else if (!SubmitResult.Success)
                            {
                                errorlogWriter.WriteLog("DBXL RER Item Deleting ERROR", SubmitResult.Errors[0].Description);
                            }
                        }
                        catch (Exception ex)
                        {
                            errorlogWriter.WriteLog("DBXL RER Item Deleting ERROR", ex.Message);
                        }
                        break;
                    }
                default:
                    {
                        errorlogWriter.WriteLog("DBXL Remote Event Receiver ERROR", "No Remote Event Type Provided.");
                        break;
                    }
            }

            clientContext.ExecuteQuery();
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

            System.Diagnostics.Trace.WriteLine("DBXL PASSWORD: " + credentials.Password);

            return DocService;
        }

        private string BuildServiceUrl()
        {
            string serviceUrl = DBXLClassLibrary.Properties.Settings.Default.GRSP_DBXL_ServiceUrl;

            serviceUrl = TokenHelper.EnsureTrailingSlash(serviceUrl);

            serviceUrl = serviceUrl + Constants.DBXL_DOC_SERVICE_PAGE;

            return serviceUrl;
        }

        private NetworkCredential BuildServiceCredentials(ClientContext clientContext)
        {
            string username = GRSPClassLibrary.Web.Dbxl.Properties.GetDbxlProperty(Constants.DBXL_USERNAME, clientContext);
            string encryptedPassword = GRSPClassLibrary.Web.Dbxl.Properties.GetDbxlProperty(Constants.DBXL_PASSWORD, clientContext);
            string decryptedPassword = GRSPClassLibrary.Web.Crypt.Decrypt(encryptedPassword);

            var credentials = new NetworkCredential(username, decryptedPassword);
            //var credentials = new NetworkCredential("db001az\johnnie.margerison", "sdW&*fnIdf32");

            return credentials;
        }

        private ListItem ClientContextListItem(ClientContext clientContext, Guid ListId, int Id)
        {
            Web Web = clientContext.Web;
            List List = Web.Lists.GetById(ListId);
            ListItem listItem = List.GetItemById(Id);

            //we need to load the listItem, will need to load and execute for the file also
            clientContext.Load(listItem);
            clientContext.ExecuteQuery();

            return listItem;
        }

        private XmlDocument LoadClientFile(ClientContext clientContext, ListItem listItem)
        {
            Microsoft.SharePoint.Client.File File = listItem.File;
            clientContext.Load(File);
            clientContext.ExecuteQuery();

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
        }
    }
}
