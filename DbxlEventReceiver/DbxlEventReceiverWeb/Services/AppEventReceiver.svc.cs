using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.EventReceivers;
using System.ServiceModel;
using GenerationReady;

namespace DbxlEventReceiverWeb.Services
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
            SPRemoteEventResult result = new SPRemoteEventResult();

            using (ClientContext clientContext = TokenHelper.CreateAppEventClientContext(properties, useAppWeb: false))
            {
                if (clientContext != null)
                {
                    clientContext.Load(clientContext.Web, web => web.Lists);
                    clientContext.ExecuteQuery();

                    if (properties.EventType == SPRemoteEventType.AppInstalled)
                    { 
                        System.Diagnostics.Trace.WriteLine(OperationContext.Current.Channel.LocalAddress.Uri.ToString());
                        GenerationReady.Diagnostics.Log.WriteLog(clientContext.Web, "RER installing", System.DateTime.Now.ToString());
                        //clientContext.Load(clientContext.Web, web => web.Title);
                        clientContext.ExecuteQuery();
                        //Response.Write(clientContext.Web.Title);
                        try
                        {
                            ListCollection Lists = clientContext.Web.Lists;
                            foreach (List List in Lists)
                            {
                                if (List.BaseTemplate.Equals(115))
                                {
                                    //List RERList = clientContext.Web.Lists.GetByTitle("Dbxl Library");
                                    // dubgging local url                    
                                    string opContext = OperationContext.Current.Channel.LocalAddress.Uri.AbsoluteUri.Substring(0, OperationContext.Current.Channel.LocalAddress.Uri.AbsoluteUri.LastIndexOf("/"));
                                    string remoteUrl = string.Format("{0}/DbxlRER.svc", opContext);

                                    //deployment url
                                    //string remoteUrl = string.Format("https://{0}/DbxlRER.svc", OperationContext.Current.Channel.LocalAddress.Uri.DnsSafeHost + "/services");
                                    EventReceiverDefinitionCreationInformation newEventReceiver = new EventReceiverDefinitionCreationInformation()
                                    {
                                        EventType = EventReceiverType.ItemAdded,
                                        ReceiverName = "DbxlRER",
                                        ReceiverUrl = remoteUrl,
                                        SequenceNumber = 1000
                                    };
                                    List.EventReceivers.Add(newEventReceiver);
                                    newEventReceiver = new EventReceiverDefinitionCreationInformation()
                                    {
                                        EventType = EventReceiverType.ItemUpdated,
                                        ReceiverName = "DbxlRER",
                                        ReceiverUrl = remoteUrl,
                                        SequenceNumber = 1000
                                    };
                                    List.EventReceivers.Add(newEventReceiver);
                                    newEventReceiver = new EventReceiverDefinitionCreationInformation()
                                    {
                                        EventType = EventReceiverType.ItemDeleting,
                                        ReceiverName = "DbxlRER",
                                        ReceiverUrl = remoteUrl,
                                        SequenceNumber = 1000
                                    };
                                    List.EventReceivers.Add(newEventReceiver);
                                    clientContext.ExecuteQuery();
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            GenerationReady.Diagnostics.Log.WriteLog(clientContext.Web, "RER installing error", ex.Message);
                            clientContext.ExecuteQuery();
                        }
                    }
                    else if (properties.EventType == SPRemoteEventType.AppUninstalling)
                    {
                        GenerationReady.Diagnostics.Log.WriteLog(clientContext.Web, "RER un-installing", System.DateTime.Now.ToString());
                        //clientContext.Load(clientContext.Web, web => web.Title);
                        clientContext.ExecuteQuery();
                        //Response.Write(clientContext.Web.Title);
                        try
                        {
                            ListCollection Lists = clientContext.Web.Lists;
                            foreach (List List in Lists)
                            {
                                if (List.BaseTemplate.Equals(115))
                                {
                                    //List RERList = clientContext.Web.Lists.GetByTitle("Dbxl Library");
                                    EventReceiverDefinitionCollection erdc = List.EventReceivers;
                                    clientContext.Load(erdc);
                                    clientContext.ExecuteQuery();
                                    List<EventReceiverDefinition> toDelete = new List<EventReceiverDefinition>();
                                    foreach (EventReceiverDefinition erd in erdc)
                                    {
                                        if (erd.ReceiverName == "DbxlRER")
                                        {
                                            toDelete.Add(erd);
                                        }
                                    }
                                    foreach (EventReceiverDefinition item in toDelete)
                                    {
                                        item.DeleteObject();
                                        clientContext.ExecuteQuery();
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            GenerationReady.Diagnostics.Log.WriteLog(clientContext.Web, "RER un-installing error", ex.Message);
                            clientContext.ExecuteQuery();
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
