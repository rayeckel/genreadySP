using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.ServiceModel;
using Microsoft.SharePoint.Client;
using System.Diagnostics;
using GRSPClassLibrary.Pages;
using GRSPClassLibrary.Web;
using DBXLEventReceiverWeb.ViewModels;

namespace DBXLEventReceiverWeb.Pages
{
    public partial class Default : AccessTokenPage
    {
        //protected void Page_PreInit(object sender, EventArgs e)
        //{
        //    Uri redirectUrl;
        //    switch (SharePointContextProvider.CheckRedirectionStatus(Context, out redirectUrl))
        //    {
        //        case RedirectionStatus.Ok:
        //            return;
        //        case RedirectionStatus.ShouldRedirect:
        //            Response.Redirect(redirectUrl.AbsoluteUri, endResponse: true);
        //            break;
        //        case RedirectionStatus.CanNotRedirect:
        //            Response.Write("An error occurred while processing your request.");
        //            Response.End();
        //            break;
        //    }
        //}

        protected void Page_Load(object sender, EventArgs e)
        {
            SetAccessToken();

            var dBXLEventReceiverVM = new DBXLEventReceiverVM(base.accessToken);
            ClientContext clientContext = dBXLEventReceiverVM.clientContext;

            SetElementProperties(clientContext);
        }

        protected void btn_SaveSettings_Click(object sender, EventArgs e)
        {
            string DbxlPropertyRerEnabled = LblListGuid.Text.ToString() + "_DbxlRerEnabled";
            string DbxlPropertyDocType = LblListGuid.Text.ToString() + "_DbxlDocType";
            string docTypeText = TxtDocType.Text;
            string REREnabled = CbxRerEnabled.Checked.ToString();

            string accessToken = this.btn_SaveSettings.CommandArgument;

            var dBXLEventReceiverVM= new DBXLEventReceiverVM(accessToken);
            dBXLEventReceiverVM.setDBXLProperties(DbxlPropertyRerEnabled, DbxlPropertyDocType, docTypeText, REREnabled);
        }

        private void SetAccessToken()
        {
            if (base.accessToken != "")
            {
                this.btn_SaveSettings.CommandArgument = base.accessToken;
            }
        }

        private void SetElementProperties(ClientContext clientContext)
        {
            //if list id present, navigated from ribbon action
            if (Request.QueryString["SPListId"] != null && !this.IsPostBack)
            {
                Guid ListGuid = new Guid(Request.QueryString["SPListId"]);

                //get list info and display
                using (clientContext)
                {
                    clientContext.Load(clientContext.Web, web => web.Url);

                    List list = clientContext.Web.Lists.GetById(ListGuid);

                    clientContext.Load(list, l => l.Title);
                    clientContext.ExecuteQuery();

                    LblListTitle.Text = list.Title;
                }

                //get current dbxl property values
                string DbxlPropertyDocType = ListGuid.ToString() + "_DbxlDocType";
                string DbxlPropertyRerEnabled = ListGuid.ToString() + "_DbxlRerEnabled";

                var dbxlPropertyDocType = GRSPClassLibrary.Web.Dbxl.Properties.GetDbxlProperty(DbxlPropertyDocType, clientContext);
                TxtDocType.Text = dbxlPropertyDocType;

                var dbxlPropertyRerEnabled = GRSPClassLibrary.Web.Dbxl.Properties.GetDbxlProperty(DbxlPropertyRerEnabled, clientContext);
                CbxRerEnabled.Checked = Convert.ToBoolean(dbxlPropertyRerEnabled);

                LblListGuid.Text = ListGuid.ToString();
                LnkHome.NavigateUrl = clientContext.Web.Url + "/default.aspx";
            }
            else if (!this.IsPostBack)
            {
                Response.Write("No List ID provided.");
            }
        }
    }
}