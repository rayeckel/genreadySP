using System;
using Microsoft.SharePoint.Client;
using System.Web;
using System.Web.UI;
using GRSPClassLibrary.Web;

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
            var sharepointUrl = new Uri(Request.QueryString[GRSPClassLibrary.Web.SharePointContext.SPHostUrlKey]);
            string contextTokenString = TokenHelper.GetContextTokenFromRequest(Page.Request);

            if (contextTokenString != null)
            {
                SharePointContextToken contextToken = TokenHelper.ReadAndValidateContextToken(contextTokenString, Request.Url.Authority);
                this.accessToken = TokenHelper.GetAccessToken(contextToken, sharepointUrl.Authority).AccessToken;
                return;
            }
            else
            {
                string appOnlyAccessToken = TokenHelper.GetAccessTokenFromAppOnlyRequest(sharepointUrl);
                if (appOnlyAccessToken != null)
                {
                    this.accessToken = appOnlyAccessToken;
                    return;
                }
            }

            if (!IsPostBack)
            {
                Response.Write("Could not find an access token.");
            }
        }
    }
}