using System;
using Microsoft.SharePoint.Client;
using System.Web;
using System.Web.UI;

namespace GRSPClassLibrary.Pages
{
    public class AccessTokenPage : Page
    {
        protected string accessToken = "";

        protected override void OnLoad(EventArgs e)
        {
            this.getSessionAccessToken();

            base.OnLoad(e);
        }

        private void getSessionAccessToken()
        {
            string contextTokenString = TokenHelper.GetContextTokenFromRequest(Page.Request);

            if (contextTokenString != null)
            {
                SharePointContextToken contextToken = TokenHelper.ReadAndValidateContextToken(contextTokenString, Request.Url.Authority);
                var sharepointUrl = new Uri(Request.QueryString["SPHostUrl"]);
                this.accessToken = TokenHelper.GetAccessToken(contextToken, sharepointUrl.Authority).AccessToken;              
            }
            else if (!IsPostBack)
            {
                Response.Write("Could not find a context token.");
            }
        }
    }
}