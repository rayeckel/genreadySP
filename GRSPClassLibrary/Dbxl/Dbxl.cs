using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.EventReceivers;
using System.Net;
using DBXLClassLibrary.DbxlDocumentService;
using GRSPClassLibrary.Base;
using GRSPClassLibrary.Web;

namespace GRSPClassLibrary.Dbxl
{
    public class Properties
    {
        private const string DBXL_PASSWORD = "DBXLPassword";
        private const string DBXL_USERNAME = "DBXLUserName";

        public static string GetDbxlProperty(string DbxlPropertyName, ClientContext clientContext)
        {
            clientContext.Load(clientContext.Web, web => web.AllProperties);
            clientContext.ExecuteQuery();
            if (clientContext.Web.AllProperties.FieldValues.ContainsKey(DbxlPropertyName))
            {
                return clientContext.Web.AllProperties[DbxlPropertyName].ToString();
            }
            return null;
        }

        public static void SetDbxlProperty(string DbxlPropertyName, string DbxlProperty, ClientContext clientContext)
        {
            clientContext.Load(clientContext.Web, web => web.AllProperties);
            clientContext.ExecuteQuery();
            clientContext.Web.AllProperties[DbxlPropertyName] = DbxlProperty;
            clientContext.Web.Update();
            clientContext.ExecuteQuery();
        }

        public static NetworkCredential BuildServiceCredentials(ClientContext clientContext)
        {
            string username = GetDbxlProperty(Properties.DBXL_USERNAME, clientContext);
            string encryptedPassword = GetDbxlProperty(Properties.DBXL_PASSWORD, clientContext);
            string decryptedPassword = Crypt.Decrypt(encryptedPassword);

            var credentials = new NetworkCredential(username, decryptedPassword);
            //var credentials = new NetworkCredential("db001az\johnnie.margerison", "sdW&*fnIdf32");

            return credentials;
        }
    }

    public class EventReceiver : GRSPEventReciever
    {
        private const string DBXL_DOC_SERVICE_PAGE = "DbxlDocumentService.asmx";
        private const string DBXL_ID_LABEL = "DbxlId";
        private const string DBXL_PROCESSING_INSTRUCTION_NAME = "QdabraDBXL";
        private const string DBXL_RECEIVER_NAME = "DbxlRER";
        private const string DBXL_SERVICE_URL_NAME = "DbxlRerServiceUrl";
        private const string KEY_DBXL_PROPERTY_RER_ENABLED = "_DbxlRerEnabled";
        public static new EventReceiverType[] eventReceiverTypes = { EventReceiverType.ItemAdded, EventReceiverType.ItemUpdated, 
                                                                   EventReceiverType.ItemUpdating, EventReceiverType.ItemDeleting };

        protected Boolean RERIsEnabled(SPRemoteEventProperties properties, ClientContext clientContext)
        {
            //check and execute if RER is enabled on list
            string DbxlRerEnabledProperty = properties.ItemEventProperties.ListId + EventReceiver.KEY_DBXL_PROPERTY_RER_ENABLED;
            Boolean RerEnabled = Convert.ToBoolean(GRSPClassLibrary.Dbxl.Properties.GetDbxlProperty(DbxlRerEnabledProperty, clientContext));
            return RerEnabled;
        }

        protected IDbxlDocumentService CredentialDocumentService(ClientContext clientContext)
        {
            string serviceUrl = BuildServiceUrl(clientContext);
            NetworkCredential credentials = GRSPClassLibrary.Dbxl.Properties.BuildServiceCredentials(clientContext);

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

        protected string BuildServiceUrl(ClientContext clientContext)
        {
            //string serviceUrl = DBXLClassLibrary.Properties.Settings.Default.GRSP_DBXL_ServiceUrl;
            string serviceUrl = GRSPClassLibrary.Dbxl.Properties.GetDbxlProperty(EventReceiver.DBXL_SERVICE_URL_NAME, clientContext);

            serviceUrl = TokenHelper.EnsureTrailingSlash(serviceUrl);

            serviceUrl = serviceUrl + EventReceiver.DBXL_DOC_SERVICE_PAGE;

            return serviceUrl;
        }

        protected XmlDocument LoadClientFile(ClientContext clientContext, ListItem listItem)
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

        protected void DbxlPiXmlProcessingInstruction(XmlDocument Doc, int DbxlId, string DbxlDocType)
        {
            String PiText = String.Format("docid=\"{0}\" doctype=\"{1}\"", DbxlId, DbxlDocType);
            XmlProcessingInstruction DbxlPi = Doc.CreateProcessingInstruction(EventReceiver.DBXL_PROCESSING_INSTRUCTION_NAME, PiText);
            Doc.AppendChild(DbxlPi);
        }
    }
}
