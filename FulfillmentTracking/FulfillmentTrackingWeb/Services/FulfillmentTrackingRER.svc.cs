using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.EventReceivers;
using GRSPClassLibrary.Web;

namespace FulfillmentTrackingWeb.Services
{
    public class FulfillmentTrackingRER : GRSPEventReciever
    {
        protected override void ExecuteRER(SPRemoteEventProperties properties, ClientContext clientContext)
        {
            clientContext.Load(clientContext.Web.Lists);
            clientContext.ExecuteQuery();

            switch (properties.EventType)
            {
                case SPRemoteEventType.ItemAdding:
                    {
                        syslogWriter.WriteLog("Fulfillment Tracking RER Fired: ", "Item Adding");
                        break;
                    }
                case SPRemoteEventType.ItemUpdating:
                    {
                        syslogWriter.WriteLog("Fulfillment Tracking RER Fired: ", "Item Updating");
                        break;
                    }
                    
                case SPRemoteEventType.ItemDeleting:
                    {
                        syslogWriter.WriteLog("Fulfillment Tracking RER Fired: ", "Item Deleting");
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }
    }
}
