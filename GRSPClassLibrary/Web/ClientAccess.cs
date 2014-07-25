using System;
using Microsoft.SharePoint.Client;

namespace GRSPClassLibrary.Web
{
    public class ClientAccess
    {
        private string SITE_URL = "https://generationreadydev.sharepoint.com/sites/re_";
        private string sessionAccessToken;

        public ClientAccess(string sessionAccessToken)
        {
            this.sessionAccessToken = sessionAccessToken;
        }

        protected ClientContext GetClientAccessContextWithToken()
        {
            return TokenHelper.GetClientContextWithAccessToken(SITE_URL, sessionAccessToken);
        }
    }
}