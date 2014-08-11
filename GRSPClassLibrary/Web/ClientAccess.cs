using System;
using Microsoft.SharePoint.Client;

namespace GRSPClassLibrary.Web
{
    public class ClientAccess
    {
        //private string sessionSPUri = "https://generationreadydev.sharepoint.com";
        protected string sessionSPUri { get; set; }
        protected string sessionAccessToken { get; set; }

        public ClientAccess(string sessionAccessToken, string sessionSPUri)
        {
            this.sessionAccessToken = sessionAccessToken;
            this.sessionSPUri = sessionSPUri;
        }

        protected ClientContext GetClientAccessContextWithToken()
        {
            return TokenHelper.GetClientContextWithAccessToken(sessionSPUri, sessionAccessToken);
        }
    }
}