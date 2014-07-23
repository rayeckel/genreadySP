using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.EventReceivers;
using GenerationReady;
using DbxlEventReceiverWeb.DbxlDocumentService;
using System.Xml;
using System.Xml.XPath;
using System.IO;
using System.Net;

namespace DbxlEventReceiverWeb.Services
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
                    GenerationReady.Diagnostics.Log.WriteLog(clientContext.Web, "RER fired", "ProcessEvent");
                    clientContext.ExecuteQuery();

                    //check and execute if RER is enabled on list
                    string DbxlRerEnabledProperty = properties.ItemEventProperties.ListId + "_DbxlRerEnabled";
                    Boolean RerEnabled = Convert.ToBoolean(GenerationReady.Dbxl.Properties.GetDbxlProperty(DbxlRerEnabledProperty, clientContext));
                    
                    if(RerEnabled)
                    { 
                        if (properties.EventType == SPRemoteEventType.ItemDeleting)
                        {
                            try
                            {
                                GenerationReady.Diagnostics.Log.WriteLog(clientContext.Web, "RER fired", "Item deleting");

                                System.Diagnostics.Trace.WriteLine("CALLING DBXL CLIENT: ITEM DELETING");

                                string ServiceUrl = "http://db001az.cloudapp.net/qdabrawebservice";
                                NetworkCredential credentials = new NetworkCredential("johnnie.margerison", "sdW&*fnIdf32");

                                IDbxlDocumentService DocService = new IDbxlDocumentService();
                                if (!ServiceUrl.EndsWith("/"))
                                    ServiceUrl = ServiceUrl + "/";
                                ServiceUrl = ServiceUrl + "DbxlDocumentService.asmx";
                                DocService.Url = ServiceUrl;
                                DocService.UseDefaultCredentials = false;
                                DocService.Credentials = credentials;
                                DocService.Timeout = 60000;

                                string DbxlRootUrl = DocService.Url;
                                System.Diagnostics.Trace.WriteLine("DBXL ROOT URL: " + DbxlRootUrl);

                                int Id = properties.ItemEventProperties.ListItemId;
                                int DbxlId;
                                //string RefId;
                                Web Web = clientContext.Web;
                                List List = Web.Lists.GetById(properties.ItemEventProperties.ListId);
                                System.Diagnostics.Trace.WriteLine(Id.ToString());
                                ListItem listItem = List.GetItemById(Id);
                                //we need to load the listItem, will need to load and execute for the file also
                                clientContext.Load(listItem);
                                clientContext.ExecuteQuery();
                                System.Diagnostics.Trace.WriteLine("DBXL ID: " + listItem["DbxlId"].ToString());
                                DbxlId = Convert.ToInt32(listItem["DbxlId"].ToString());

                                StatusInfo info = DocService.RemoveDocument(DbxlId);

                                if (info.Success)
                                {
                                    System.Diagnostics.Trace.WriteLine("RESULT: " + info.Success.ToString());
                                }
                                else if (!info.Success)
                                {
                                    System.Diagnostics.Trace.WriteLine("RESULT: " + info.Success.ToString());
                                    //System.Diagnostics.Trace.WriteLine("ERROR CODE: " + info.Errors[0].Code);
                                    //System.Diagnostics.Trace.WriteLine("ERROR DESCRIPTION: " + SubmitResult.Errors[0].Description);
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Trace.WriteLine("MESSAGE: " + ex.Message);
                                System.Diagnostics.Trace.WriteLine("SOURCE: " + ex.Source);
                                System.Diagnostics.Trace.WriteLine("INNER EXCEPTION: " + ex.InnerException);
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
                    GenerationReady.Diagnostics.Log.WriteLog(clientContext.Web, "RER fired", "ProcessOneWayEvent");
                    clientContext.ExecuteQuery();
                    
                    //check and execute if RER is enabled on list
                    string DbxlRerEnabledProperty = properties.ItemEventProperties.ListId + "_DbxlRerEnabled";
                    Boolean RerEnabled = Convert.ToBoolean(GenerationReady.Dbxl.Properties.GetDbxlProperty(DbxlRerEnabledProperty, clientContext));

                    if (RerEnabled)
                    {
                        //get Dbxl document type for list
                        string DbxlDocTypeProperty = properties.ItemEventProperties.ListId + "_DbxlDocType";
                        string DbxlDocType = GenerationReady.Dbxl.Properties.GetDbxlProperty(DbxlDocTypeProperty, clientContext);

                        if (properties.EventType == SPRemoteEventType.ItemAdded)
                        {
                            try
                            {
                                GenerationReady.Diagnostics.Log.WriteLog(clientContext.Web, "RER fired", "Item added");

                                System.Diagnostics.Trace.WriteLine("CALLING DBXL CLIENT: ITEM ADDED");

                                string ServiceUrl = "http://db001az.cloudapp.net/qdabrawebservice";
                                NetworkCredential credentials = new NetworkCredential("johnnie.margerison", "sdW&*fnIdf32");

                                IDbxlDocumentService DocService = new IDbxlDocumentService();
                                if (!ServiceUrl.EndsWith("/"))
                                    ServiceUrl = ServiceUrl + "/";
                                ServiceUrl = ServiceUrl + "DbxlDocumentService.asmx";
                                DocService.Url = ServiceUrl;
                                DocService.UseDefaultCredentials = false;
                                DocService.Credentials = credentials;
                                DocService.Timeout = 60000;

                                string DbxlRootUrl = DocService.Url;
                                System.Diagnostics.Trace.WriteLine("DBXL ROOT URL: " + DbxlRootUrl);

                                int Id = properties.ItemEventProperties.ListItemId;
                                int DbxlId;
                                string RefId;
                                Web Web = clientContext.Web;
                                List List = Web.Lists.GetById(properties.ItemEventProperties.ListId);
                                System.Diagnostics.Trace.WriteLine(Id.ToString());
                                ListItem listItem = List.GetItemById(Id);
                                //we need to load the listItem, will need to load and execute for the file also
                                clientContext.Load(listItem);
                                clientContext.ExecuteQuery();

                                Microsoft.SharePoint.Client.File File = listItem.File;
                                clientContext.Load(File);
                                clientContext.ExecuteQuery();

                                System.Diagnostics.Trace.WriteLine("FILE ID: " + listItem.Id.ToString());
                                System.Diagnostics.Trace.WriteLine("FILE LENGTH: " + File.Length.ToString());

                                ClientResult<System.IO.Stream> Stream = File.OpenBinaryStream();
                                clientContext.ExecuteQuery();

                                XmlTextReader text_reader = new XmlTextReader(Stream.Value);
                                XmlDocument Doc = new XmlDocument();
                                Doc.Load(text_reader);
                                //StatusInfo SubmitResult = DocService.SubmitDocument("DbxlTestAlpha", Doc.OuterXml, Id.ToString(), "Author", "Alpha", "True", out DbxlId, out RefId);
                                StatusInfo SubmitResult = DocService.SubmitDocument(DbxlDocType, Doc.OuterXml, Id.ToString(), "Author", "Alpha", "True", out DbxlId, out RefId);

                                System.Diagnostics.Trace.WriteLine("SUCCESS: " + SubmitResult.Success.ToString());
                                if (SubmitResult.Success)
                                {
                                    System.Diagnostics.Trace.WriteLine("DBXL ID: " + DbxlId.ToString());
                                    listItem["DbxlId"] = DbxlId.ToString();
                                    listItem.Update();
                                    clientContext.ExecuteQuery();
                                }
                                else if (!SubmitResult.Success)
                                {
                                    System.Diagnostics.Trace.WriteLine("ERROR CODE: " + SubmitResult.Errors[0].Code);
                                    System.Diagnostics.Trace.WriteLine("ERROR DESCRIPTION: " + SubmitResult.Errors[0].Description);
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Trace.WriteLine("MESSAGE: " + ex.Message);
                                System.Diagnostics.Trace.WriteLine("SOURCE: " + ex.Source);
                                System.Diagnostics.Trace.WriteLine("INNER EXCEPTION: " + ex.InnerException);
                                //Diagnostics.WriteLog(clientContext.Web, "RER item adding error", ex.Message);
                                //clientContext.ExecuteQuery();
                            }
                        }
                        if (properties.EventType == SPRemoteEventType.ItemUpdated)
                        {
                            try
                            {
                                GenerationReady.Diagnostics.Log.WriteLog(clientContext.Web, "RER fired", "Item updated");

                                System.Diagnostics.Trace.WriteLine("CALLING DBXL CLIENT: ITEM UDPATED");

                                string ServiceUrl = "http://db001az.cloudapp.net/qdabrawebservice";
                                NetworkCredential credentials = new NetworkCredential("johnnie.margerison", "sdW&*fnIdf32");

                                IDbxlDocumentService DocService = new IDbxlDocumentService();
                                if (!ServiceUrl.EndsWith("/"))
                                    ServiceUrl = ServiceUrl + "/";
                                ServiceUrl = ServiceUrl + "DbxlDocumentService.asmx";
                                DocService.Url = ServiceUrl;
                                DocService.UseDefaultCredentials = false;
                                DocService.Credentials = credentials;
                                DocService.Timeout = 60000;

                                string DbxlRootUrl = DocService.Url;
                                System.Diagnostics.Trace.WriteLine("DBXL ROOT URL: " + DbxlRootUrl);

                                int Id = properties.ItemEventProperties.ListItemId;
                                int DbxlId;
                                string RefId;
                                Web Web = clientContext.Web;
                                List List = Web.Lists.GetById(properties.ItemEventProperties.ListId);
                                System.Diagnostics.Trace.WriteLine(Id.ToString());
                                ListItem listItem = List.GetItemById(Id);
                                //we need to load the listItem, will need to load and execute for the file also
                                clientContext.Load(listItem);
                                clientContext.ExecuteQuery();
                                System.Diagnostics.Trace.WriteLine("DBXL ID: " + listItem["DbxlId"].ToString());
                                DbxlId = Convert.ToInt32(listItem["DbxlId"].ToString());

                                Microsoft.SharePoint.Client.File File = listItem.File;
                                clientContext.Load(File);
                                clientContext.ExecuteQuery();

                                System.Diagnostics.Trace.WriteLine("FILE ID: " + listItem.Id.ToString());
                                System.Diagnostics.Trace.WriteLine("FILE LENGTH: " + File.Length.ToString());

                                ClientResult<System.IO.Stream> Stream = File.OpenBinaryStream();
                                clientContext.ExecuteQuery();

                                XmlTextReader text_reader = new XmlTextReader(Stream.Value);
                                XmlDocument Doc = new XmlDocument();
                                Doc.Load(text_reader);

                                //add qdabra processing instruction
                                XmlProcessingInstruction DbxlPi;
                                String PiText = String.Format("docid=\"{0}\" doctype=\"{1}\"", DbxlId, DbxlDocType);
                                DbxlPi = Doc.CreateProcessingInstruction("QdabraDBXL", PiText);
                                System.Diagnostics.Trace.WriteLine(String.Format("PROCESSING INSTRUCTION: {0}, {1}", DbxlPi.Target, DbxlPi.Data));
                                Doc.AppendChild(DbxlPi);
                                System.Diagnostics.Trace.WriteLine(Doc.OuterXml);

                                //StatusInfo SubmitResult = DocService.SubmitDocument("DbxlTestAlpha", Doc.OuterXml, Id.ToString(), "Author", "Alpha", "True", out DbxlId, out RefId);
                                StatusInfo SubmitResult = DocService.SubmitDocument(DbxlDocType, Doc.OuterXml, Id.ToString(), "Author", "Alpha", "True", out DbxlId, out RefId);

                                System.Diagnostics.Trace.WriteLine("REFID: " + RefId.ToString());
                                System.Diagnostics.Trace.WriteLine("SUCCESS: " + SubmitResult.Success.ToString());
                                if (SubmitResult.Success)
                                {
                                    System.Diagnostics.Trace.WriteLine("DBXL ID: " + DbxlId.ToString());
                                }
                                else if (!SubmitResult.Success)
                                {
                                    System.Diagnostics.Trace.WriteLine("ERROR CODE: " + SubmitResult.Errors[0].Code);
                                    System.Diagnostics.Trace.WriteLine("ERROR DESCRIPTION: " + SubmitResult.Errors[0].Description);
                                }

                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Trace.WriteLine("MESSAGE: " + ex.Message);
                                System.Diagnostics.Trace.WriteLine("SOURCE: " + ex.Source);
                                System.Diagnostics.Trace.WriteLine("INNER EXCEPTION: " + ex.InnerException);
                            }
                        }
                    }
                }
            }
        }
    }
}
