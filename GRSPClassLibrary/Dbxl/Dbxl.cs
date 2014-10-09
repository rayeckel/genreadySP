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
        public static NetworkCredential BuildServiceCredentials(ClientContext clientContext)
        {
            string username = WebUtils.GetAppProperty(Constants.DBXL_USERNAME, clientContext);
            string encryptedPassword = WebUtils.GetAppProperty(Constants.DBXL_PASSWORD, clientContext);
            string decryptedPassword = Crypt.Decrypt(encryptedPassword);

            var credentials = new NetworkCredential(username, decryptedPassword);
            //var credentials = new NetworkCredential("db001az\johnnie.margerison", "sdW&*fnIdf32");

            return credentials;
        }
    }

    public class EventReceiver : GRSPEventReciever
    {
        private const string DBXL_DOC_SERVICE_PAGE = "DbxlDocumentService.asmx";
        private const string DBXL_PROCESSING_INSTRUCTION_NAME = "QdabraDBXL";
        public static new EventReceiverType[] eventReceiverTypes = { EventReceiverType.ItemAdded, EventReceiverType.ItemUpdated, 
                                                                   EventReceiverType.ItemUpdating, EventReceiverType.ItemDeleting };

        protected Boolean RERIsEnabled(SPRemoteEventProperties properties, ClientContext clientContext)
        {
            //check and execute if RER is enabled on list
            string DbxlRerEnabledProperty = properties.ItemEventProperties.ListId + Constants.KEY_DBXL_PROPERTY_RER_ENABLED;
            Boolean RerEnabled = Convert.ToBoolean(WebUtils.GetAppProperty(DbxlRerEnabledProperty, clientContext));
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
            string serviceUrl = WebUtils.GetAppProperty(Constants.DBXL_SERVICE_URL_NAME, clientContext);

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
