using System;
using Microsoft.SharePoint.Client;
using GRSPClassLibrary.Web;

namespace SystemLogs.Log
{
    public class LogWriter : ClientAccess
    {
        private string LOG_TITLE = "System Log";

        public LogWriter(string sessionAccessToken) : base(sessionAccessToken)
        {}

        public void WriteLog(string Title, string Description)
        {
            var clientContext = base.GetClientAccessContextWithToken();
            using (clientContext)
            {
                ListItem SystemLogListItem = this.GenerateSystemLogListItem(clientContext);

                SystemLogListItem["Title"] = Title;
                SystemLogListItem["Description"] = Description;

                SystemLogListItem.Update();

                clientContext.ExecuteQuery();
            }
        }

        public void WriteLog(string Title, string Description, string Detail)
        {
            var clientContext = base.GetClientAccessContextWithToken();
            using (clientContext)
            {
                ListItem SystemLogListItem = this.GenerateSystemLogListItem(clientContext);

                SystemLogListItem["Title"] = Title;
                SystemLogListItem["Description"] = Description;
                SystemLogListItem["Detail"] = Detail;

                SystemLogListItem.Update();

                clientContext.ExecuteQuery();
            }
        }

        private ListItem GenerateSystemLogListItem(ClientContext clientContext)
        {
            Microsoft.SharePoint.Client.Web oWebsite = clientContext.Web;
            clientContext.Load(oWebsite);

            List SystemLogList = oWebsite.Lists.GetByTitle(LOG_TITLE);

            var ListItemCreateInfo = new ListItemCreationInformation();
            ListItem SystemLogListItem = SystemLogList.AddItem(ListItemCreateInfo);

            return SystemLogListItem;
        }
    }
}