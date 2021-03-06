﻿using System;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.EventReceivers;
using GRSPClassLibrary.Base;
using GRSPClassLibrary.Web.Log;

namespace GRSPClassLibrary.Web
{
    public partial class GRSPEventReciever : IRemoteEventService
    {
        private const string SYSTEM_LOG_LABEL = "System Log";
        private const string ERROR_LOG_LABEL = "Error Log";
        protected SPRemoteEventResult result = new SPRemoteEventResult();
        protected LogWriter syslogWriter;
        protected LogWriter errorlogWriter;
        public static EventReceiverType[] eventReceiverTypes =
            { EventReceiverType.ItemAdding, EventReceiverType.ItemUpdating, EventReceiverType.ItemDeleting, 
                EventReceiverType.ItemAdded, EventReceiverType.ItemUpdated };

        /// <summary>
        /// Handles events that occur before an action occurs, such as when a user adds or deletes a list item.
        /// </summary>
        /// <param name="properties">Holds information about the remote event.</param>
        /// <returns>Holds information returned from the remote event.</returns>
        public SPRemoteEventResult ProcessEvent(SPRemoteEventProperties properties)
        {
            BuildLoggingContext(properties);
            syslogWriter.WriteLog("Fulfillment Tracking RER DEBUG", "Begin ProcessEvent", properties.ItemEventProperties.ListItemId);

            ClientContext clientContext = GetClientContext(properties);

            if (clientContext != null)
            {
                //BuildLoggingContext(properties);

                using (clientContext)
                {
                    ExecuteRER(properties, clientContext);
                }
            }
            else
            {
                syslogWriter.WriteLog("Fulfillment Tracking RER DEBUG", "clientContext is NULL", properties.ItemEventProperties.ListItemId);

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

        protected virtual void ExecuteRER(SPRemoteEventProperties properties, ClientContext clientContext)
        {
            throw new NotImplementedException();
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

            syslogWriter = new LogWriter(GRSPEventReciever.SYSTEM_LOG_LABEL, loggingContext);
            errorlogWriter = new LogWriter(GRSPEventReciever.ERROR_LOG_LABEL, loggingContext);
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

        protected void ExecuteQuery(ClientContext clientContext, int ListId = 0)
        {
            try
            {
                syslogWriter.WriteLog("Fulfillment Tracking RER DEBUG", "Executing Query", ListId);
                clientContext.ExecuteQuery();
            }
            catch (Exception ex)
            {
                errorlogWriter.WriteLog("RER EXECUTE QUERY ERROR", ex.Message);
            }
        }
    }
}
