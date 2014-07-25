using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.SharePoint.Client;
using GRSPClassLibrary.Web;

namespace DBXLEventReceiverWeb.ViewModels
{
    public class DBXLEventReceiverData : ClientAccess
    {
        public ClientContext clientContext
        {
            get
            {
                return base.GetClientAccessContextWithToken();
            }
        }

        public DBXLEventReceiverData(string sessionAccessToken)
            : base(sessionAccessToken)
        {

        }

        public void setDBXLProperties(string DbxlPropertyRerEnabled, string DbxlPropertyDocType, string docTypeText, string REREnabled)
        {
            using (clientContext)
            {
                GRSPClassLibrary.Web.Dbxl.Properties.SetDbxlProperty(DbxlPropertyDocType, docTypeText, clientContext);
                GRSPClassLibrary.Web.Dbxl.Properties.SetDbxlProperty(DbxlPropertyRerEnabled, REREnabled, clientContext);
            }
        }
    }
}