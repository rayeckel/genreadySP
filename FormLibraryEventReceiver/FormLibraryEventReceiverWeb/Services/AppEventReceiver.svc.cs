using System;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.EventReceivers;
using System.ServiceModel;
using FormLibraryEventReceiverWeb.Base;
using GRSPClassLibrary.Web;

namespace FormLibraryEventReceiverWeb.Services
{
    public class AppEventReceiver : GRSPEventReciever
    {
        private const string DBXL_RECEIVER_NAME = "DbxlRER";

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

                            foreach (List webList in webLists)
                            {
                                if (webList.BaseTemplate.Equals(115))
                                {
                                    foreach (var receiverType in GRSPClassLibrary.Dbxl.EventReceiver.eventReceiverTypes)
                                    {
                                        webList.EventReceivers.Add(new EventReceiverDefinitionCreationInformation()
                                        {
                                            EventType = receiverType,
                                            ReceiverName = AppEventReceiver.DBXL_RECEIVER_NAME,
                                            ReceiverUrl = remoteUrl,
                                            SequenceNumber = 1000
                                        });
                                    }
                                }
                            }

                            syslogWriter.WriteLog("App RER application installed", " : SUCCESS");

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
                                        if (erd.ReceiverName == AppEventReceiver.DBXL_RECEIVER_NAME)
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
