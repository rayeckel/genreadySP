using System;
using System.ServiceModel;
using System.Linq;
using System.Text;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.EventReceivers;
using FulfillmentWeb.Base;
using GRSPClassLibrary.Web;

namespace FulfillmentWeb.Services
{
    public class AppEventReceiver : GRSPEventReciever
    {
        private const string FULFILLMENT_TRACKING_RECEIVER_NAME = "FulfillmentTrackingRER";

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
            clientContext.Load(clientContext.Web, web => web.Lists);
            clientContext.ExecuteQuery();

            ListCollection webLists = clientContext.Web.Lists;
            List trackingList = webLists.GetByTitle(Constants.TRACKING_LIBRARY_NAME);

            if (trackingList != null)
            {
                switch (properties.EventType)
                {
                    case SPRemoteEventType.AppInstalled:
                        {
                            try
                            {
                                string opContext = OperationContext.Current.Channel.LocalAddress.Uri.AbsoluteUri.Substring(0, OperationContext.Current.Channel.LocalAddress.Uri.AbsoluteUri.LastIndexOf("/"));
                                string remoteUrl = string.Format("{0}/{1}.svc", opContext, AppEventReceiver.FULFILLMENT_TRACKING_RECEIVER_NAME);

                                foreach (var receiverType in eventReceiverTypes)
                                {
                                    trackingList.EventReceivers.Add(new EventReceiverDefinitionCreationInformation()
                                    {
                                        EventType = receiverType,
                                        ReceiverName = AppEventReceiver.FULFILLMENT_TRACKING_RECEIVER_NAME,
                                        ReceiverUrl = remoteUrl,
                                        SequenceNumber = 1000
                                    });
                                }

                                syslogWriter.WriteLog("Fulfillment Tracking RER Item Added to: ", Constants.TRACKING_LIBRARY_NAME);
                            }

                            catch (Exception ex)
                            {
                                errorlogWriter.WriteLog("Fulfillment Tracking RER Item Added ERROR", ex.Message);
                            }

                            break;
                        }
                    case SPRemoteEventType.AppUninstalling:
                        {
                            try
                            {
                                EventReceiverDefinitionCollection erdCollection = trackingList.EventReceivers;
                                clientContext.Load(erdCollection);
                                clientContext.ExecuteQuery();

                                foreach (EventReceiverDefinition erd in erdCollection)
                                {
                                    if (erd.ReceiverName == AppEventReceiver.FULFILLMENT_TRACKING_RECEIVER_NAME)
                                    {
                                        erd.DeleteObject();
                                    }
                                }

                                syslogWriter.WriteLog("Fulfillment Tracking RER Item Removed from: ", Constants.TRACKING_LIBRARY_NAME);
                            }

                            catch (Exception ex)
                            {
                                errorlogWriter.WriteLog("Fulfillment Tracking RER Item Updated ERROR", ex.Message);
                            }

                            break;
                        }
                }
            }

            clientContext.ExecuteQuery();
        }
    }
}
