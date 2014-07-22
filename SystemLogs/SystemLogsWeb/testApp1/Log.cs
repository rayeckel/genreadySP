using System;
using Microsoft.SharePoint.Client;

namespace SystemLogs.LogWriter
{
    public class Log
    {
        private string LOG_TITLE = "System Log";
        private string SITE_URL = "https://generationreadydev.sharepoint.com/sites/re_/logtest";
        private string accessToken;

        public Log(string sessionAccessToken)
        {
            accessToken = sessionAccessToken;
        }

        public void WriteLog(string Title, string Description)
        {
            using (var clientContext = TokenHelper.GetClientContextWithAccessToken(SITE_URL, accessToken))
            {
                ListItem SystemLogListItem = GenerateSystemLogListItem(clientContext);

                SystemLogListItem["Title"] = Title;
                SystemLogListItem["Description"] = Description;

                SystemLogListItem.Update();

                clientContext.ExecuteQuery();
            }
        }

        public void WriteLog(string Title, string Description, string Detail)
        {
            using (var clientContext = TokenHelper.GetClientContextWithAccessToken(SITE_URL, accessToken))
            {
                ListItem SystemLogListItem = GenerateSystemLogListItem(clientContext);

                SystemLogListItem["Title"] = Title;
                SystemLogListItem["Description"] = Description;
                SystemLogListItem["Detail"] = Detail;

                SystemLogListItem.Update();

                clientContext.ExecuteQuery();
            }
        }

        private ListItem GenerateSystemLogListItem(ClientContext clientContext)
        {
            Web oWebsite = clientContext.Web;
            clientContext.Load(oWebsite);

            List SystemLogList = oWebsite.Lists.GetByTitle(LOG_TITLE);

            var ListItemCreateInfo = new ListItemCreationInformation();
            ListItem SystemLogListItem = SystemLogList.AddItem(ListItemCreateInfo);

            return SystemLogListItem;
        }
    }
}