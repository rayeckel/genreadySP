using System;
using Microsoft.SharePoint.Client;

namespace GRSPClassLibrary.Web.Log
{
    public class LogWriter : ClientAccess
    {
        #region Contructors
        public LogWriter(string logTitle, string sessionAccessToken)
            : base(sessionAccessToken)
        {
            this.logTitle = logTitle;
        }

        #endregion

        #region Properties
        private string _logTitle { get; set; }
        private string logTitle 
        { 
            get { return _logTitle; }
            set
            {
                if(String.IsNullOrEmpty(value)) 
                {
                    throw new SystemException("No log title was provided.");
                }
                else
                {
                    _logTitle = value;
                }
            }
        }

        #endregion

        #region Methods
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

            List SystemLogList = oWebsite.Lists.GetByTitle(logTitle);

            var ListItemCreateInfo = new ListItemCreationInformation();
            ListItem SystemLogListItem = SystemLogList.AddItem(ListItemCreateInfo);

            return SystemLogListItem;
        }

        #endregion
    }
}