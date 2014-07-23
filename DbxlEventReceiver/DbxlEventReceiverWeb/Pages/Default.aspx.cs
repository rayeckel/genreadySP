using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using GenerationReady;
using System.ServiceModel;
using Microsoft.SharePoint.Client;
using System.Diagnostics;

namespace DbxlEventReceiverWeb
{
    public partial class Default : System.Web.UI.Page
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
            // The following code gets the client context and Title property by using TokenHelper.
            // To access other properties, the app may need to request permissions on the host web.
            var spContext = SharePointContextProvider.Current.GetSharePointContext(Context);

            using (var clientContext = spContext.CreateUserClientContextForSPHost())
            {
                clientContext.Load(clientContext.Web, web => web.Title, web => web.Url, web => web.AllProperties);
                clientContext.ExecuteQuery();
                Response.Write(clientContext.Web.Title);
                //set home url for web navigation
                LnkHome.NavigateUrl = clientContext.Web.Url + "/default.aspx";

                //if list id present, navigated from ribbon action
                if (Request.QueryString["SPListId"] != null)
                {
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
                        TxtDocType.Text = GenerationReady.Dbxl.GetDbxlProperty(DbxlPropertyDocType, clientContext);
                        CbxRerEnabled.Checked = Convert.ToBoolean(GenerationReady.Dbxl.GetDbxlProperty(DbxlPropertyRerEnabled, clientContext));
                    }
                    else if (Page.IsPostBack)
                    {
                        if (ViewState["TxtDocType"] != null)
                            TxtDocType.Text = ViewState["TxtDocType"].ToString();
                    }
                }
            }
        }

        protected void SaveDbxlSettings(object sender, EventArgs e)
        {
            var spContext = SharePointContextProvider.Current.GetSharePointContext(Context);

            using (var clientContext = spContext.CreateUserClientContextForSPHost())
            {
                string DbxlPropertyRerEnabled = LblListGuid.Text.ToString() + "_DbxlRerEnabled";
                string DbxlPropertyDocType = LblListGuid.Text.ToString() + "_DbxlDocType";
                GenerationReady.Dbxl.SetDbxlProperty(DbxlPropertyDocType, TxtDocType.Text, clientContext);
                GenerationReady.Dbxl.SetDbxlProperty(DbxlPropertyRerEnabled, CbxRerEnabled.Checked.ToString(), clientContext);
            }
        }
    }
}