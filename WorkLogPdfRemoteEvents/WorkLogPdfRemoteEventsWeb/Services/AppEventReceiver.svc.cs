using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.EventReceivers;
using GRSPClassLibrary.Web;
using WorkLogPdfRemoteEventsWeb.Base;

namespace WorkLogPdfRemoteEventsWeb.Services
{
    public class AppEventReceiver : GRSPEventReciever
    {
        private const string WORKLOGS_RECEIVER_NAME = "WorkLogsDocumentRER";

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
            List trackingList = webLists.GetByTitle(Constants.WORKLOGS_LIBRARY_NAME);

            if (trackingList != null)
            {
                switch (properties.EventType)
                {
                    case SPRemoteEventType.AppInstalled:
                        {
                            try
                            {
                                string opContext = OperationContext.Current.Channel.LocalAddress.Uri.AbsoluteUri.Substring(0, OperationContext.Current.Channel.LocalAddress.Uri.AbsoluteUri.LastIndexOf("/"));
                                string remoteUrl = string.Format("{0}/{1}.svc", opContext, AppEventReceiver.WORKLOGS_RECEIVER_NAME);

                                foreach (var receiverType in eventReceiverTypes)
                                {
                                    trackingList.EventReceivers.Add(new EventReceiverDefinitionCreationInformation()
                                    {
                                        EventType = receiverType,
                                        ReceiverName = AppEventReceiver.WORKLOGS_RECEIVER_NAME,
                                        ReceiverUrl = remoteUrl,
                                    });
                                }

                                syslogWriter.WriteLog("Work Logs Document RER Item Added to: ", Constants.WORKLOGS_LIBRARY_NAME);
                            }

                            catch (Exception ex)
                            {
                                errorlogWriter.WriteLog("Work Logs Document RER Item Added ERROR", ex.Message);
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
                                    if (erd.ReceiverName == AppEventReceiver.WORKLOGS_RECEIVER_NAME)
                                    {
                                        erd.DeleteObject();
                                    }
                                }

                                syslogWriter.WriteLog("Work Logs Document RER Item Removed from: ", Constants.WORKLOGS_LIBRARY_NAME);
                            }

                            catch (Exception ex)
                            {
                                errorlogWriter.WriteLog("Work Logs Document RER Item Updated triggered", ex.Message);
                            }

                            break;
                        }
                }
            }

            clientContext.ExecuteQuery();
        }
    }
}
