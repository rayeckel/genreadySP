using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.EventReceivers;
using GRSPClassLibrary;
using DBXLClassLibrary.DbxlDocumentService;
using System.Xml;
using System.Xml.XPath;
using System.IO;
using System.Net;

namespace DBXLEventReceiverWeb.Services
{
    public class DbxlRER : IRemoteEventService
    {
        /// <summary>
        /// Handles events that occur before an action occurs, such as when a user adds or deletes a list item.
        /// </summary>
        /// <param name="properties">Holds information about the remote event.</param>
        /// <returns>Holds information returned from the remote event.</returns>
        public SPRemoteEventResult ProcessEvent(SPRemoteEventProperties properties)
        {
            SPRemoteEventResult result = new SPRemoteEventResult();

            using (ClientContext clientContext = TokenHelper.CreateRemoteEventReceiverClientContext(properties))
            {
                if (clientContext != null)
                {
                    clientContext.Load(clientContext.Web);
                    clientContext.ExecuteQuery();

                    ExecuteRER(properties, clientContext);
                }
            }

            return result;
        }

        /// <summary>
        /// Handles events that occur after an action occurs, such as after a user adds an item to a list or deletes an item from a list.
        /// </summary>
        /// <param name="properties">Holds information about the remote event.</param>
        public void ProcessOneWayEvent(SPRemoteEventProperties properties)
        {
            using (ClientContext clientContext = TokenHelper.CreateRemoteEventReceiverClientContext(properties))
            {
                if (clientContext != null)
                {
                    clientContext.Load(clientContext.Web);
                    clientContext.ExecuteQuery();
                }
            }
        }

        private void ExecuteRER(SPRemoteEventProperties properties, ClientContext clientContext)
        {
            //check and execute if RER is enabled on list
            string DbxlRerEnabledProperty = properties.ItemEventProperties.ListId + "_DbxlRerEnabled";
            Boolean RerEnabled = Convert.ToBoolean(GRSPClassLibrary.Web.Dbxl.Properties.GetDbxlProperty(DbxlRerEnabledProperty, clientContext));

            if (RerEnabled)
            {
                if (properties.EventType == SPRemoteEventType.ItemDeleting)
                {
                    try
                    {
                        //GenerationReady.Diagnostics.Log.WriteLog(clientContext.Web, "RER fired", "Item deleting");
                        //System.Diagnostics.Trace.WriteLine("CALLING DBXL CLIENT: ITEM DELETING");

                        IDbxlDocumentService DocService = CredentialDocumentService();

                        Guid listId = properties.ItemEventProperties.ListId;
                        int Id = properties.ItemEventProperties.ListItemId;
                        ListItem listItem = ClientContextListItem(clientContext, listId, Id);

                        int DbxlId = Convert.ToInt32(listItem["DbxlId"].ToString());
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
                }
            }
            /*
            if (properties.EventType == SPRemoteEventType.ItemAdding)
            {
                try
                {
                    Diagnostics.WriteLog(clientContext.Web, "RER fired", "Item adding");

                    System.Diagnostics.Trace.WriteLine("CALLING DBXL CLIENT");

                    DbxlClient client = new DbxlClient("http://db001az.cloudapp.net/qdabrawebservice/");
                    string DbxlRootUrl = client.DbxlDocumentService.DbxlRootUrl;
                    string DbxlVersion = client.DbxlAdmin.GetDbxlVersion();
                    //Diagnostics.WriteLog(clientContext.Web, "DBXL event", DbxlVersion);
                    System.Diagnostics.Trace.WriteLine("DBXL ROOT URL: " + DbxlRootUrl);
                    System.Diagnostics.Trace.WriteLine("DBXL VERSION: " + DbxlVersion);
                            
                    int Id = properties.ItemEventProperties.ListItemId;
                    int DbxlId;
                    string RefId;
                    Web Web = clientContext.Web;
                    List List = Web.Lists.GetById(properties.ItemEventProperties.ListId);
                    ListItem ListItem = List.GetItemById(Id);
                    System.Diagnostics.Trace.WriteLine(properties.ItemEventProperties.BeforeUrl);
                    Microsoft.SharePoint.Client.File File = Web.GetFileByServerRelativeUrl(properties.ItemEventProperties.BeforeUrl);
                    ClientResult<Stream> Stream = File.OpenBinaryStream();
                            
                    XPathDocument Doc = new XPathDocument(Stream.Value);
                    StatusInfo SubmitResult = client.DbxlDocumentService.SubmitDocument("DbxlTestAlpha", Doc.ToString(), Id.ToString(), "Author", "Alpha", out DbxlId, out RefId);

                    if (SubmitResult.Success)
                    {
                        result.ChangedItemProperties.Add("Dbxl Id", DbxlId);
                        result.Status = SPRemoteEventServiceStatus.Continue;
                    }
                            
                    //System.Diagnostics.Trace.WriteLine(SubmitResult.ToString());

                    /*
                    //we use the ChangedItemProperties to adjust a field value whilst item is adding
                    result.ChangedItemProperties.Add("Dbxl Id", "RER: " + System.DateTime.Now.ToString());
                    result.Status = SPRemoteEventServiceStatus.Continue;
                            
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(ex.Message);
                    //Diagnostics.WriteLog(clientContext.Web, "RER item adding error", ex.Message);
                    //clientContext.ExecuteQuery();
                }
            }*/
        }

        private IDbxlDocumentService CredentialDocumentService()
        {
            string ServiceUrl = "http://db001az.cloudapp.net/qdabrawebservice";
            var credentials = new NetworkCredential("johnnie.margerison", "sdW&*fnIdf32");
            var DocService = new IDbxlDocumentService();

            if (!ServiceUrl.EndsWith("/"))
            {
                ServiceUrl = ServiceUrl + "/";
            }

            ServiceUrl = ServiceUrl + "DbxlDocumentService.asmx";
            DocService.Url = ServiceUrl;
            DocService.UseDefaultCredentials = false;
            DocService.Credentials = credentials;
            DocService.Timeout = 60000;

            string DbxlRootUrl = DocService.Url;
            //System.Diagnostics.Trace.WriteLine("DBXL ROOT URL: " + DbxlRootUrl);

            return DocService;
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

            XmlTextReader text_reader = new XmlTextReader(Stream.Value);
            XmlDocument Doc = new XmlDocument();
            Doc.Load(text_reader);

            return Doc;
        }

        private void DbxlPiXmlProcessingInstruction(XmlDocument Doc, int DbxlId, string DbxlDocType)
        {
            String PiText = String.Format("docid=\"{0}\" doctype=\"{1}\"", DbxlId, DbxlDocType);
            XmlProcessingInstruction DbxlPi = Doc.CreateProcessingInstruction("QdabraDBXL", PiText);
            Doc.AppendChild(DbxlPi);

            //System.Diagnostics.Trace.WriteLine(String.Format("PROCESSING INSTRUCTION: {0}, {1}", DbxlPi.Target, DbxlPi.Data));
            //System.Diagnostics.Trace.WriteLine(Doc.OuterXml);
        }
    }
}
