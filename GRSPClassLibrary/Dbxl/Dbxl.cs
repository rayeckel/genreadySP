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
            string username = GetDbxlProperty(Constants.DBXL_USERNAME, clientContext);
            string encryptedPassword = GetDbxlProperty(Constants.DBXL_PASSWORD, clientContext);
            string decryptedPassword = Crypt.Decrypt(encryptedPassword);

            var credentials = new NetworkCredential(username, decryptedPassword);
            //var credentials = new NetworkCredential("db001az\johnnie.margerison", "sdW&*fnIdf32");

            return credentials;
        }
    }

    public class EventReceiver : GRSPEventReciever
    {
        public static new EventReceiverType[] eventReceiverTypes = { EventReceiverType.ItemAdded, EventReceiverType.ItemUpdated, 
                                                                   EventReceiverType.ItemUpdating, EventReceiverType.ItemDeleting };

        protected Boolean RERIsEnabled(SPRemoteEventProperties properties, ClientContext clientContext)
        {
            //check and execute if RER is enabled on list
            string DbxlRerEnabledProperty = properties.ItemEventProperties.ListId + Constants.KEY_DBXL_PROPERTY_RER_ENABLED;
            Boolean RerEnabled = Convert.ToBoolean(GRSPClassLibrary.Dbxl.Properties.GetDbxlProperty(DbxlRerEnabledProperty, clientContext));
            return RerEnabled;
        }

        protected IDbxlDocumentService CredentialDocumentService(ClientContext clientContext)
        {
            string serviceUrl = BuildServiceUrl();
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

        protected string BuildServiceUrl()
        {
            string serviceUrl = DBXLClassLibrary.Properties.Settings.Default.GRSP_DBXL_ServiceUrl;

            serviceUrl = TokenHelper.EnsureTrailingSlash(serviceUrl);

            serviceUrl = serviceUrl + Constants.DBXL_DOC_SERVICE_PAGE;

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
            XmlProcessingInstruction DbxlPi = Doc.CreateProcessingInstruction(Constants.DBXL_PROCESSING_INSTRUCTION_NAME, PiText);
            Doc.AppendChild(DbxlPi);
        }
    }
}
