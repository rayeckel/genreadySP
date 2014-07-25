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
using GRSPClassLibrary.Web.Log;
using DBXLEventReceiverWeb.ViewModels;

namespace DBXLEventReceiverWeb.Pages
{
    public partial class Default : AccessTokenPage
    {
        protected void Page_PreInit(object sender, EventArgs e)
        {
            //Uri redirectUrl;
            //switch (SharePointContextProvider.CheckRedirectionStatus(Context, out redirectUrl))
            //{
            //    case RedirectionStatus.Ok:
            //        return;
            //    case RedirectionStatus.ShouldRedirect:
            //        Response.Redirect(redirectUrl.AbsoluteUri, endResponse: true);
            //        break;
            //    case RedirectionStatus.CanNotRedirect:
            //        Response.Write("An error occurred while processing your request.");
            //        Response.End();
            //        break;
            //}
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            setAccessToken();

            var dBXLEventReceiverData = new DBXLEventReceiverData(base.accessToken);
            ClientContext clientContext = dBXLEventReceiverData.clientContext;

            setElementProperties(clientContext);
        }

        protected void btn_SaveSettings_Click(object sender, EventArgs e)
        {
            string DbxlPropertyRerEnabled = LblListGuid.Text.ToString() + "_DbxlRerEnabled";
            string DbxlPropertyDocType = LblListGuid.Text.ToString() + "_DbxlDocType";
            string docTypeText = TxtDocType.Text;
            string REREnabled = CbxRerEnabled.Checked.ToString();

            string accessToken = this.btn_SaveSettings.CommandArgument;
            var dBXLEventReceiverData = new DBXLEventReceiverData(accessToken);

            dBXLEventReceiverData.setDBXLProperties(DbxlPropertyRerEnabled, DbxlPropertyDocType, docTypeText, REREnabled);
        }

        private void setAccessToken()
        {
            if (base.accessToken != "")
            {
                this.btn_SaveSettings.CommandArgument = base.accessToken;
            }
        }

        private void writeToLog(string Title, string Description)
        {
            string accessToken = this.btn_SaveSettings.CommandArgument;
            var logWriter = new LogWriter("System Log", accessToken);
            logWriter.WriteLog(Title, Description);
        }

        private void setElementProperties(ClientContext clientContext)
        {
            //if list id present, navigated from ribbon action
            if (Request.QueryString["SPListId"] != null)
            {
                using (clientContext)
                {
                    clientContext.Load(clientContext.Web, web => web.Title, web => web.Url, web => web.AllProperties);

                    //get list info and display
                    Guid ListGuid = new Guid(Request.QueryString["SPListId"]);
                    List list = clientContext.Web.Lists.GetById(ListGuid);

                    clientContext.Load(list, l => l.Title);
                    clientContext.ExecuteQuery();

                    LblListGuid.Text = ListGuid.ToString();
                    LblListTitle.Text = list.Title;

                    //get current dbxl property values
                    string DbxlPropertyRerEnabled = ListGuid.ToString() + "_DbxlRerEnabled";
                    string DbxlPropertyDocType = ListGuid.ToString() + "_DbxlDocType";

                    if (!Page.IsPostBack)
                    {
                        var dbxlPropertyDocType = GRSPClassLibrary.Web.Dbxl.Properties.GetDbxlProperty(DbxlPropertyDocType, clientContext);
                        TxtDocType.Text = dbxlPropertyDocType;

                        var dbxlPropertyRerEnabled = GRSPClassLibrary.Web.Dbxl.Properties.GetDbxlProperty(DbxlPropertyRerEnabled, clientContext);
                        CbxRerEnabled.Checked = Convert.ToBoolean(dbxlPropertyRerEnabled);
                    }
                    else if (Page.IsPostBack)
                    {
                        if (ViewState["TxtDocType"] != null)
                        {
                            TxtDocType.Text = ViewState["TxtDocType"].ToString();
                        }
                    }

                    //set home url for web navigation
                    LnkHome.NavigateUrl = clientContext.Web.Url + "/default.aspx";
                }
            }
            else
            {
                Response.Write("No List ID provided.");
            }
        }
    }
}