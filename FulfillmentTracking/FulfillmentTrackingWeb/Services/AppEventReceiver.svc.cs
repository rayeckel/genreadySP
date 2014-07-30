using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.EventReceivers;
using GRSPClassLibrary.Web;

namespace FulfillmentTrackingWeb.Services
{
    public class AppEventReceiver : GRSPEventReciever
    {
        protected override void ExecuteRER(SPRemoteEventProperties properties, ClientContext clientContext)
        {
            clientContext.Load(clientContext.Web.Lists);
            clientContext.ExecuteQuery();

            switch (properties.EventType)
            {
                case SPRemoteEventType.AppInstalled:
                    {
                    }
                    break;
            }
        }
    }
}
