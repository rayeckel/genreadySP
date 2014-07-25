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
        public void setDBXLProperties(Dictionary<string, string> formVariables)
        {
            //Encrypt the password.
            string passwordVar = formVariables[Constants.DBXL_PASSWORD];
            formVariables[Constants.DBXL_PASSWORD] = GRSPClassLibrary.Web.Crypt.Encrypt(passwordVar);

            using (clientContext)
            {
                foreach (KeyValuePair<string, string> variable in formVariables)
                {
                    GRSPClassLibrary.Web.Dbxl.Properties.SetDbxlProperty(variable.Key, variable.Value, clientContext);
                }
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