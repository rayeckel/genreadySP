using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.EventReceivers;
using System.ServiceModel;
using GRSPClassLibrary.Web.Log;
using GRSPClassLibrary.Web;

namespace DBXLEventReceiverWeb.Services
{
    public class AppEventReceiver : IRemoteEventService
    {
        /// <summary>
        /// Handles app events that occur after the app is installed or upgraded, or when app is being uninstalled.
        /// </summary>
        /// <param name="properties">Holds information about the app event.</param>
        /// <returns>Holds information returned from the app event.</returns>
        public SPRemoteEventResult ProcessEvent(SPRemoteEventProperties properties)
        {
            var result = new SPRemoteEventResult();
            ClientContext clientContext = GetClientContext(properties);

            using (clientContext)
            {
                if (clientContext != null)
                {
                    ProcessEventType(properties, clientContext);
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

        private void ProcessEventType(SPRemoteEventProperties properties, ClientContext clientContext)
        {
            clientContext.Load(clientContext.Web.Lists);
            clientContext.ExecuteQuery();

            ListCollection webLists = clientContext.Web.Lists;

            var logWriter = new GRSPClassLibrary.Web.Log.LogWriter("System Log", clientContext);
            logWriter.WriteLog("RER APP installed", "App Installed: Position 1");

            switch (properties.EventType)
            {
                case SPRemoteEventType.AppInstalled:
                    {
                        //System.Diagnostics.Trace.WriteLine(OperationContext.Current.Channel.LocalAddress.Uri.ToString());
                        //LogWriter.WriteLog("RER installing", System.DateTime.Now.ToString());
                        try
                        {
                            foreach (List webList in webLists)
                            {
                                if (webList.BaseTemplate.Equals(115))
                                {
                                    AddEventRecieversToList(webList);
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            //GenerationReady.Diagnostics.Log.WriteLog(clientContext.Web, "RER installing error", ex.Message);
                        }
                        break;
                    }
                case SPRemoteEventType.AppUninstalling:
                    {
                        //GenerationReady.Diagnostics.Log.WriteLog(clientContext.Web, "RER un-installing", System.DateTime.Now.ToString());
                        try
                        {
                            foreach (List webList in webLists)
                            {
                                if (webList.BaseTemplate.Equals(115))
                                {
                                    //List RERList = clientContext.Web.Lists.GetByTitle("Dbxl Library");
                                    EventReceiverDefinitionCollection erdCollection = webList.EventReceivers;
                                    clientContext.Load(erdCollection);
                                    clientContext.ExecuteQuery();

                                    foreach (EventReceiverDefinition erd in erdCollection)
                                    {
                                        if (erd.ReceiverName == "DbxlRER")
                                        {
                                            erd.DeleteObject();
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            //GenerationReady.Diagnostics.Log.WriteLog(clientContext.Web, "RER un-installing error", ex.Message);
                        }
                        break;
                    }
            }

            clientContext.ExecuteQuery();
        }

        private ClientContext GetClientContext(SPRemoteEventProperties properties)
        {
            string webUrl = properties.AppEventProperties.HostWebFullUrl.ToString();
            var sharepointUrl = new Uri(webUrl);

            string appOnlyAccessToken = TokenHelper.GetAccessTokenFromAppOnlyRequest(sharepointUrl);
            ClientContext clientContext = TokenHelper.GetClientContextWithAccessToken(webUrl, appOnlyAccessToken);

            return clientContext;
        }

        private void AddEventRecieversToList(List list)
        {
            //List RERList = clientContext.Web.Lists.GetByTitle("Dbxl Library");
            //dubgging local url                    
            string opContext = OperationContext.Current.Channel.LocalAddress.Uri.AbsoluteUri.Substring(0, OperationContext.Current.Channel.LocalAddress.Uri.AbsoluteUri.LastIndexOf("/"));
            string remoteUrl = string.Format("{0}/DbxlRER.svc", opContext);

            //deployment url
            //string remoteUrl = string.Format("https://{0}/DbxlRER.svc", OperationContext.Current.Channel.LocalAddress.Uri.DnsSafeHost + "/services");
            list.EventReceivers.Add(new EventReceiverDefinitionCreationInformation()
            {
                EventType = EventReceiverType.ItemAdded,
                ReceiverName = "DbxlRER",
                ReceiverUrl = remoteUrl,
                SequenceNumber = 1000
            });

            list.EventReceivers.Add(new EventReceiverDefinitionCreationInformation()
            {
                EventType = EventReceiverType.ItemUpdated,
                ReceiverName = "DbxlRER",
                ReceiverUrl = remoteUrl,
                SequenceNumber = 1000
            });

            list.EventReceivers.Add(new EventReceiverDefinitionCreationInformation()
            {
                EventType = EventReceiverType.ItemDeleting,
                ReceiverName = "DbxlRER",
                ReceiverUrl = remoteUrl,
                SequenceNumber = 1000
            });
        }
    }
}
