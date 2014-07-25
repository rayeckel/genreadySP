using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.SharePoint.Client;
using GRSPClassLibrary.Web;
using GRSPClassLibrary.Web.Log;

namespace DBXLEventReceiverWeb.ViewModels
{
    public class DBXLEventReceiverVM : ClientAccess
    {
        #region Properties
        public ClientContext clientContext
        {
            get
            {
                return base.GetClientAccessContextWithToken();
            }
        }

        #endregion

        #region Constructors

        public DBXLEventReceiverVM(string sessionAccessToken)
            : base(sessionAccessToken)
        {

        }

        #endregion

        #region Methods
        public void setDBXLProperties(string DbxlPropertyRerEnabled, string DbxlPropertyDocType, string docTypeText, string REREnabled)
        {
            using (clientContext)
            {
                GRSPClassLibrary.Web.Dbxl.Properties.SetDbxlProperty(DbxlPropertyDocType, docTypeText, clientContext);
                GRSPClassLibrary.Web.Dbxl.Properties.SetDbxlProperty(DbxlPropertyRerEnabled, REREnabled, clientContext);
            }
        }

        private void writeToLog(string Title, string Description)
        {
            var logWriter = new LogWriter("System Log", base.sessionAccessToken);
            logWriter.WriteLog(Title, Description);
        }

        #endregion
    }
}