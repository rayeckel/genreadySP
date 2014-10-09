using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.SharePoint.Client;
using GRSPClassLibrary.Pages;
using GRSPClassLibrary.Web;
using ReportsAppPartWeb.Base;
using ReportsAppPartWeb.ViewModels;

namespace ReportsAppPartWeb.Pages.Home
{
    public partial class ReportsApp : AccessTokenPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            SetAccessToken();
            string spUri = base.GetSharepointUri();
            string accessToken = base.accessToken;

            var reportsAppVM = new ReportsAppVM(accessToken, spUri);
            ClientContext clientContext = reportsAppVM.clientContext;
        }

        protected void btn_SaveSettings_Click(object sender, EventArgs e)
        {
            Dictionary<string, string> formVariables = new Dictionary<string, string>()
            {
                { Constants.REPORTSERVER_URL_LABEL, rptServerUrl.Text },
                { Constants.REPORTSERVER_USERNAME_LABEL, TxtUsername.Text },
                { Constants.REPORTSERVER_PASSWORD_LABEL, TxtPassword.Text }
            };

            string spUri = base.GetSharepointUri();
            string accessToken = this.btn_SaveSettings.CommandArgument;
            
            var reportsAppVM = new ReportsAppVM(accessToken, spUri);
            reportsAppVM.setReportProperties(formVariables);
        }

        private void SetAccessToken()
        {
            if (!String.IsNullOrEmpty(base.accessToken))
            {
                this.btn_SaveSettings.CommandArgument = base.accessToken;
            }
        }
    }
}