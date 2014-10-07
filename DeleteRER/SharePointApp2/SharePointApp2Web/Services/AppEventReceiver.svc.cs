using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.EventReceivers;
using System.ServiceModel;

namespace SharePointApp2Web.Services
{
    public class AppEventReceiver : IRemoteEventService
    {
        public static EventReceiverType[] eventReceiverTypes =
            { EventReceiverType.ItemAdding, EventReceiverType.ItemUpdating, EventReceiverType.ItemDeleting, 
                EventReceiverType.ItemAdded, EventReceiverType.ItemUpdated };
        /// <summary>
        /// Handles app events that occur after the app is installed or upgraded, or when app is being uninstalled.
        /// </summary>
        /// <param name="properties">Holds information about the app event.</param>
        /// <returns>Holds information returned from the app event.</returns>
        public SPRemoteEventResult ProcessEvent(SPRemoteEventProperties properties)
        {
            SPRemoteEventResult result = new SPRemoteEventResult();

            using (ClientContext clientContext = TokenHelper.CreateAppEventClientContext(properties, useAppWeb: false))
            {
                if (clientContext != null)
                {
                    clientContext.Load(clientContext.Web, web => web.Lists);
                    clientContext.ExecuteQuery();

                    ListCollection webLists = clientContext.Web.Lists;

                    switch (properties.EventType)
                    {
                        case SPRemoteEventType.AppInstalled:
                            {
                                try
                                {
                                    string opContext = OperationContext.Current.Channel.LocalAddress.Uri.AbsoluteUri.Substring(0, OperationContext.Current.Channel.LocalAddress.Uri.AbsoluteUri.LastIndexOf("/"));
                                    string remoteUrl = string.Format("{0}/RemoteEventReceiver1.svc", opContext);

                                    foreach (List webList in webLists)
                                    {
                                            foreach (var receiverType in eventReceiverTypes)
                                            {
                                                webList.EventReceivers.Add(new EventReceiverDefinitionCreationInformation()
                                                {
                                                    EventType = receiverType,
                                                    ReceiverName = "RemoteEventReceiver1",
                                                    ReceiverUrl = remoteUrl,
                                                    SequenceNumber = 1000
                                                });
                                            }
                                    }

                                }
                                catch (Exception ex)
                                {

                                }
                                break;
                            }
                    }

                }
            }

            return result;
        }

        /// <summary>
        /// This method is a required placeholder, but is not used by app events.
        /// </summary>
        /// <param name="properties">Unused.</param>
        public void ProcessOneWayEvent(SPRemoteEventProperties properties)
        {
            throw new NotImplementedException();
        }

    }
}
