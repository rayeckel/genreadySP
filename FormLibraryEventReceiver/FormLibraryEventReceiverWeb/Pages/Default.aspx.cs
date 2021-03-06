﻿using System;
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
using FormLibraryEventReceiverWeb.Base;
using FormLibraryEventReceiverWeb.ViewModels;

namespace FormLibraryEventReceiverWeb.Pages
{
    public partial class Default : AccessTokenPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            SetAccessToken();
            string spUri = base.GetSharepointUri();
            string accessToken = base.accessToken;

            var formLibraryEventReceiverVM = new FormLibraryEventReceiverVM(accessToken, spUri);
            ClientContext clientContext = formLibraryEventReceiverVM.clientContext;

            SetElementProperties(clientContext);
        }

        protected void btn_SaveSettings_Click(object sender, EventArgs e)
        {
            string enabledKey = LblListGuid.Text.ToString() + GRSPClassLibrary.Base.Constants.KEY_DBXL_PROPERTY_RER_ENABLED;
            string docTypeKey = LblListGuid.Text.ToString() + GRSPClassLibrary.Base.Constants.KEY_DBXL_PROPERTY_DOCTYPE;

            Dictionary<string, string> formVariables = new Dictionary<string, string>()
            {
                { enabledKey, CbxRerEnabled.Checked.ToString() },
                { docTypeKey, TxtDocType.Text },
                { GRSPClassLibrary.Base.Constants.DBXL_SERVICE_URL_NAME, TxtServiceUrl.Text },
                { GRSPClassLibrary.Base.Constants.DBXL_USERNAME, TxtUsername.Text },
                { GRSPClassLibrary.Base.Constants.DBXL_PASSWORD, TxtPassword.Text }
            };

            string spUri = base.GetSharepointUri();
            string accessToken = this.btn_SaveSettings.CommandArgument;

            var formLibraryEventReceiverVM = new FormLibraryEventReceiverVM(accessToken, spUri);
            formLibraryEventReceiverVM.setDBXLProperties(formVariables);
        }

        private void SetAccessToken()
        {
            if (!String.IsNullOrEmpty(base.accessToken))
            {
                this.btn_SaveSettings.CommandArgument = base.accessToken;
            }
        }

        private void SetElementProperties(ClientContext clientContext)
        {
            //if list id present, navigated from ribbon action
            if (Request.QueryString[Constants.SP_QUERY_STRING_LIST_ID] != null && !this.IsPostBack)
            {
                var ListGuid = new Guid(Request.QueryString[Constants.SP_QUERY_STRING_LIST_ID]);

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
                string DbxlPropertyDocType = ListGuid.ToString() + GRSPClassLibrary.Base.Constants.KEY_DBXL_PROPERTY_DOCTYPE;
                string DbxlPropertyRerEnabled = ListGuid.ToString() + GRSPClassLibrary.Base.Constants.KEY_DBXL_PROPERTY_RER_ENABLED;

                var dbxlPropertyDocType = GRSPClassLibrary.Dbxl.Properties.GetDbxlProperty(DbxlPropertyDocType, clientContext);
                TxtDocType.Text = dbxlPropertyDocType;

                var dbxlPropertyRerEnabled = GRSPClassLibrary.Dbxl.Properties.GetDbxlProperty(DbxlPropertyRerEnabled, clientContext);
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