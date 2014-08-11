using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using Microsoft.SharePoint.Client;
using GRSPClassLibrary.Base;
using GRSPClassLibrary.Web;
using GRSPClassLibrary.Web.Log;
using FormLibraryEventReceiverWeb.Base;

namespace FormLibraryEventReceiverWeb.ViewModels
{
    public class FormLibraryEventReceiverVM : ClientAccess
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

        public FormLibraryEventReceiverVM(string sessionAccessToken, string sessionSPUri)
            : base(sessionAccessToken, sessionSPUri)
        {

        }

        #endregion

        #region Methods
        public void setDBXLProperties(Dictionary<string, string> formVariables)
        {
            //Encrypt the password.
            string passwordVar = formVariables[GRSPClassLibrary.Base.Constants.DBXL_PASSWORD];
            formVariables[GRSPClassLibrary.Base.Constants.DBXL_PASSWORD] = GRSPClassLibrary.Web.Crypt.Encrypt(passwordVar);

            using (clientContext)
            {
                foreach (KeyValuePair<string, string> variable in formVariables)
                {
                    GRSPClassLibrary.Dbxl.Properties.SetDbxlProperty(variable.Key, variable.Value, clientContext);
                }
            }
        }

        private void writeToLog(string Title, string Description)
        {
            var logWriter = new LogWriter("System Log", base.sessionAccessToken, base.sessionSPUri);
            logWriter.WriteLog(Title, Description);
        }

        #endregion
    }
}