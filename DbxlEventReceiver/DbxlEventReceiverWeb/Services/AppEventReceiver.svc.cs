using System;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.EventReceivers;
using System.ServiceModel;
using DBXLEventReceiverWeb.Base;
using GRSPClassLibrary.Web;

namespace DBXLEventReceiverWeb.Services
{
    public class AppEventReceiver : GRSPEventReciever
    {
        /// <summary>
        /// This method is a required placeholder, but is not used by app events.
        /// </summary>
        /// <param name="properties">Unused.</param>
        public override void ProcessOneWayEvent(SPRemoteEventProperties properties)
        {
            throw new NotImplementedException();
        }

        protected override void ExecuteRER(SPRemoteEventProperties properties, ClientContext clientContext)
        {
            clientContext.Load(clientContext.Web.Lists);
            clientContext.ExecuteQuery();

            ListCollection webLists = clientContext.Web.Lists;

            switch (properties.EventType)
            {
                case SPRemoteEventType.AppInstalled:
                    {
                        try
                        {
                            string opContext = OperationContext.Current.Channel.LocalAddress.Uri.AbsoluteUri.Substring(0, OperationContext.Current.Channel.LocalAddress.Uri.AbsoluteUri.LastIndexOf("/"));
                            string remoteUrl = string.Format("{0}/DbxlRER.svc", opContext);

                            EventReceiverType[] EventReceiverTypes = { EventReceiverType.ItemAdded, EventReceiverType.ItemUpdated, EventReceiverType.ItemDeleting };

                            foreach (List webList in webLists)
                            {
                                if (webList.BaseTemplate.Equals(115))
                                {
                                    foreach (var receiverType in EventReceiverTypes)
                                    {
                                        webList.EventReceivers.Add(new EventReceiverDefinitionCreationInformation()
                                        {
                                            EventType = receiverType,
                                            ReceiverName = Constants.DBXL_RECEIVER_NAME,
                                            ReceiverUrl = remoteUrl,
                                            SequenceNumber = 1000
                                        });
                                    }
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            errorlogWriter.WriteLog("App RER application installed ERROR", ex.Message);
                        }
                        break;
                    }
                case SPRemoteEventType.AppUninstalling:
                    {
                        try
                        {
                            foreach (List webList in webLists)
                            {
                                if (webList.BaseTemplate.Equals(115))
                                {
                                    EventReceiverDefinitionCollection erdCollection = webList.EventReceivers;
                                    clientContext.Load(erdCollection);
                                    clientContext.ExecuteQuery();

                                    foreach (EventReceiverDefinition erd in erdCollection)
                                    {
                                        if (erd.ReceiverName == Constants.DBXL_RECEIVER_NAME)
                                        {
                                            erd.DeleteObject();
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            errorlogWriter.WriteLog("App RER application un-installing ERROR", ex.Message);
                        }
                        break;
                    }
            }

            clientContext.ExecuteQuery();
        }
    }
}
