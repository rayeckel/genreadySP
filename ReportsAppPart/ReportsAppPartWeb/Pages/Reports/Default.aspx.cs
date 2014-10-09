using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.SharePoint.Client;
using GRSPClassLibrary.Pages;
using GRSPClassLibrary.Web;
using ReportsAppPartWeb.Base;
using ReportsAppPartWeb.ViewModels;

namespace ReportsAppPartWeb
{
    public partial class Default : AccessTokenPage
    {
        protected void Page_PreInit(object sender, EventArgs e)
        {
            Uri redirectUrl;
            switch (SharePointContextProvider.CheckRedirectionStatus(Context, out redirectUrl))
            {
                case RedirectionStatus.Ok:
                    return;
                case RedirectionStatus.ShouldRedirect:
                    Response.Redirect(redirectUrl.AbsoluteUri, endResponse: true);
                    break;
                case RedirectionStatus.CanNotRedirect:
                    Response.Write("An error occurred while processing your request.");
                    Response.End();
                    break;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack)
            {
                //When the report loads, it triggers a re-load of the page, so this catches that.
                return;
            }

            string spUri = base.GetSharepointUri();
            string accessToken = base.accessToken;

            var reportsAppVM = new ReportsAppVM(accessToken, spUri);
            ClientContext clientContext = reportsAppVM.clientContext;

            SetupReport(clientContext);
        }

        private void SetupReport(ClientContext clientContext)
        {
            //// Set the processing mode for the ReportViewer to Remote
            reportViewer1.ProcessingMode = Microsoft.Reporting.WebForms.ProcessingMode.Remote;

            Microsoft.Reporting.WebForms.ServerReport serverReport = reportViewer1.ServerReport;

            //// Set the report server credentials
            var ReportServerUsername = GRSPClassLibrary.Web.WebUtils.GetAppProperty(Constants.REPORTSERVER_USERNAME_LABEL, clientContext);
            var ReportServerPassword = GRSPClassLibrary.Web.WebUtils.GetAppProperty(Constants.REPORTSERVER_PASSWORD_LABEL, clientContext);
            var ReportServerPasswordDecrypted = GRSPClassLibrary.Web.Crypt.Decrypt(ReportServerPassword);
            serverReport.ReportServerCredentials = new CustomReportCredentials(ReportServerUsername, ReportServerPasswordDecrypted); 

            //// Set the report server URL and report path; 
            var rptServerUrl = GRSPClassLibrary.Web.WebUtils.GetAppProperty(Constants.REPORTSERVER_URL_LABEL, clientContext);
            serverReport.ReportServerUrl = new Uri(rptServerUrl);

            string reportParam = string.Format("{0}{1}", "/", Request.QueryString["reportName"]);
            serverReport.ReportPath = reportParam;

            bool showTB = false;
            string showTBParam = Request.QueryString["showToolBar"];

            if (showTBParam != null && showTBParam == "true")
            {
                showTB = true;
            }

            reportViewer1.ShowToolBar = showTB;
        }
    }

    public class CustomReportCredentials : Microsoft.Reporting.WebForms.IReportServerCredentials
    {
        // local variable for network credential.
        private string _UserName;
        private string _PassWord;
        public CustomReportCredentials(string UserName, string PassWord)
        {
            _UserName = UserName;
            _PassWord = PassWord;
        }
        public WindowsIdentity ImpersonationUser
        {
            get
            {
                return null;  // not use ImpersonationUser
            }
        }
        public ICredentials NetworkCredentials
        {
            get
            {

                // use NetworkCredentials
                return new NetworkCredential(_UserName, _PassWord);
            }
        }
        public bool GetFormsCredentials(out Cookie authCookie, out string user, out string password, out string authority)
        {

            // not use FormsCredentials unless you have implements a custom autentication.
            authCookie = null;
            user = password = authority = null;
            return false;
        }
    }
}