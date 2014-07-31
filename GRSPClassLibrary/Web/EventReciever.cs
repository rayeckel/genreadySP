using System;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.EventReceivers;
using GRSPClassLibrary.Base;
using GRSPClassLibrary.Web.Log;

namespace GRSPClassLibrary.Web
{
    public partial class GRSPEventReciever : IRemoteEventService
    {
        protected LogWriter syslogWriter;
        protected LogWriter errorlogWriter;

        /// <summary>
        /// Handles events that occur before an action occurs, such as when a user adds or deletes a list item.
        /// </summary>
        /// <param name="properties">Holds information about the remote event.</param>
        /// <returns>Holds information returned from the remote event.</returns>
        public SPRemoteEventResult ProcessEvent(SPRemoteEventProperties properties)
        {
            var result = new SPRemoteEventResult();
            ClientContext clientContext = GetClientContext(properties);

            if (clientContext != null)
            {
                BuildLoggingContext(properties);

                using (clientContext)
                {
                    ExecuteRER(properties, clientContext);
                }
            }

            return result;
        }

        /// <summary>
        /// Handles events that occur after an action occurs, such as after a user adds an item to a list or deletes an item from a list.
        /// </summary>
        /// <param name="properties">Holds information about the remote event.</param>
        public virtual void ProcessOneWayEvent(SPRemoteEventProperties properties)
        {
            ProcessEvent(properties);
        }

        private ClientContext GetClientContext(SPRemoteEventProperties properties)
        {
            string webUrl;

            if (properties.AppEventProperties != null)
            {
                webUrl = properties.AppEventProperties.HostWebFullUrl.ToString();
            }
            else if (properties.ItemEventProperties != null)
            {
                webUrl = properties.ItemEventProperties.WebUrl.ToString();
            }
            else
            {
                throw new System.ApplicationException("Host web URL not available");
            }

            var sharepointUrl = new Uri(webUrl);

            string appOnlyAccessToken = TokenHelper.GetAccessTokenFromAppOnlyRequest(sharepointUrl);
            ClientContext clientContext = TokenHelper.GetClientContextWithAccessToken(webUrl, appOnlyAccessToken);

            return clientContext;
        }

        private void BuildLoggingContext(SPRemoteEventProperties properties)
        {
            ClientContext loggingContext = GetClientContext(properties);

            syslogWriter = new LogWriter(Constants.SYSTEM_LOG_LABEL, loggingContext);
            errorlogWriter = new LogWriter(Constants.ERROR_LOG_LABEL, loggingContext);
        }

        protected virtual void ExecuteRER(SPRemoteEventProperties properties, ClientContext clientContext)
        {
            throw new NotImplementedException();
        }

        public ListItem ClientContextListItem(ClientContext clientContext, Guid ListId, int Id)
        {
            Microsoft.SharePoint.Client.Web Web = clientContext.Web;
            List List = Web.Lists.GetById(ListId);
            ListItem listItem = List.GetItemById(Id);

            //we need to load the listItem, will need to load and execute for the file also
            clientContext.Load(listItem);
            clientContext.ExecuteQuery();

            return listItem;
        }
    }
}
