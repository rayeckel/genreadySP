using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.SharePoint.Client;
using ReportsAppPartWeb.Base;
using GRSPClassLibrary.Web;
using GRSPClassLibrary.Web.Log;

namespace ReportsAppPartWeb.ViewModels
{
    public class ReportsAppVM : ClientAccess
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
        public ReportsAppVM(string sessionAccessToken, string sessionSPUri)
            : base(sessionAccessToken, sessionSPUri)
        {}
        
        #endregion

        #region Methods
        public void setReportProperties(Dictionary<string, string> formVariables)
        {
            //Encrypt the password.
            string passwordVar = formVariables[Constants.REPORTSERVER_PASSWORD_LABEL];
            formVariables[Constants.REPORTSERVER_PASSWORD_LABEL] = GRSPClassLibrary.Web.Crypt.Encrypt(passwordVar);

            using (clientContext)
            {
                foreach (KeyValuePair<string, string> variable in formVariables)
                {
                    GRSPClassLibrary.Web.WebUtils.SetAppProperty(variable.Key, variable.Value, clientContext);
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