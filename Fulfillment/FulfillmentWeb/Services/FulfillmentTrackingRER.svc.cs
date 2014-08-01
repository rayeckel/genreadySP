using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.EventReceivers;
using FulfillmentWeb.Base;
using GRSPClassLibrary.Web;

namespace FulfillmentWeb.Services
{
    public class FulfillmentTrackingRER : GRSPEventReciever
    {
        protected override void ExecuteRER(SPRemoteEventProperties properties, ClientContext clientContext)
        {
            clientContext.Load(clientContext.Web, web => web.Lists);
            clientContext.ExecuteQuery();

            Guid listId = properties.ItemEventProperties.ListId;
            int Id = properties.ItemEventProperties.ListItemId;        

            ListCollection webLists = clientContext.Web.Lists;
            List trackingList = webLists.GetById(listId);
            List articlesList = webLists.GetByTitle(Constants.ARTICLES_LIBRARY_NAME);
            List allocationsList = webLists.GetByTitle(Constants.ALLOCATIONS_LIBRARY_NAME);

            //var q = allocationsList.GetItems(articleQuery).First();

            if (properties.EventType == SPRemoteEventType.ItemAdded || properties.EventType == SPRemoteEventType.ItemUpdated)
            {
                ListItem listItem = ClientContextListItem(clientContext, listId, Id);
                var articleId = (string)listItem[Constants.LIST_ITEM_ARTICLE_ID];

                var articleQuery = new CamlQuery();
                articleQuery.ViewXml = "<View><Query><Where><Geq><FieldRef Name='ArticleId'/>" +
                "<Value Type='Number'>" + articleId + "</Value></Geq></Where></Query><RowLimit>1</RowLimit></View>";

                if (properties.EventType == SPRemoteEventType.ItemAdded)
                {
                    try
                    {
                        syslogWriter.WriteLog("Fulfillment Tracking RER triggered", "Item Added");

                        listItem[Constants.LIST_ITEM_ARTICLE_ID] = "999999";
                        listItem.Update();
                        clientContext.ExecuteQuery();
                    }
                    catch (Exception ex)
                    {
                        errorlogWriter.WriteLog("Fulfillment Tracking RER Item Added ERROR", ex.Message);
                    }
                }
                else
                {
                    try
                    {
                        syslogWriter.WriteLog("Fulfillment Tracking RER  triggered", "Item Updated");
                    }
                    catch (Exception ex)
                    {
                        errorlogWriter.WriteLog("Fulfillment Tracking RER Item Updated triggered", ex.Message);
                    }
                }
            }
            else if (properties.EventType == SPRemoteEventType.ItemDeleting)
            {
                try
                {
                    syslogWriter.WriteLog("Fulfillment Tracking RER  triggered", "Item Deleting");
                }
                catch (Exception ex)
                {
                    errorlogWriter.WriteLog("Fulfillment Tracking RER Item Deleting ERROR", ex.Message);
                }
            }
            else
            {
                errorlogWriter.WriteLog("Fulfillment Tracking Remote Event Receiver ERROR", "Event Type Not Handled.");
            }

            clientContext.ExecuteQuery();
        }
    }
}
