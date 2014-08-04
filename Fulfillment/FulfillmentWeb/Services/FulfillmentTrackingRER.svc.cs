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

            //Guid listId = properties.ItemEventProperties.ListId;
            //int Id = properties.ItemEventProperties.ListItemId;        

            updateAllocationsListITem(clientContext, properties);
            updateArticlesListITem(clientContext, properties);

            switch (properties.EventType)
            {
                case SPRemoteEventType.ItemAdding:
                    {
                        try
                        {
                            syslogWriter.WriteLog("Fulfillment Tracking RER triggered", "Item Added");
                        }
                        catch (Exception ex)
                        {
                            errorlogWriter.WriteLog("Fulfillment Tracking RER Item Added ERROR", ex.Message);
                        }

                        break;
                    }
                case SPRemoteEventType.ItemUpdating:
                    {
                        try
                        {
                            syslogWriter.WriteLog("Fulfillment Tracking RER  triggered", "Item Updated");
                        }
                        catch (Exception ex)
                        {
                            errorlogWriter.WriteLog("Fulfillment Tracking RER Item Updated triggered", ex.Message);
                        }

                        break;
                    }
                case SPRemoteEventType.ItemDeleting:
                    {
                        try
                        {
                            syslogWriter.WriteLog("Fulfillment Tracking RER  triggered", "Item Deleting");
                        }
                        catch (Exception ex)
                        {
                            errorlogWriter.WriteLog("Fulfillment Tracking RER Item Deleting ERROR", ex.Message);
                        }

                        break;
                    }
                default:
                    {
                        break;
                    }
            }

            //clientContext.ExecuteQuery();
        }

        private void updateAllocationsListITem(ClientContext clientContext, SPRemoteEventProperties properties)
        {
            ListCollection webLists = clientContext.Web.Lists;
            List allocationsList = webLists.GetByTitle(Constants.ALLOCATIONS_LIBRARY_NAME);

            var allocationId = Convert.ToString(properties.ItemEventProperties.AfterProperties[Constants.LIST_ITEM_ALLOCATION_ID]);
            var oldAllocationId = Convert.ToString(properties.ItemEventProperties.AfterProperties[Constants.INPUT_PREVIOUS_ALLOCATION_ID]);
            var units = Convert.ToDecimal(properties.ItemEventProperties.AfterProperties[Constants.INPUT_UNIT]);

            var allocationQuery = new CamlQuery();
            allocationQuery.ViewXml = "<View><Query><Where><Geq><FieldRef Name='ID'/>" +
                "<Value Type='Number'>" + allocationId + "</Value></Geq></Where></Query><RowLimit>1</RowLimit></View>";
            var query = allocationsList.GetItems(allocationQuery);
            clientContext.Load(query);
            clientContext.ExecuteQuery();

            if (query.Count() > 0)
            {
                var allocationListItem = query.First();
                var oldUnits = Convert.ToDecimal(allocationListItem[Constants.LIST_ITEM_ALLOCATIONS_FULFILLED]);
                var oldRemaining = Convert.ToDecimal(allocationListItem[Constants.LIST_ITEM_ALLOCATIONS_REMAINING]);

                var newRemaining = oldRemaining - units;
                var fulfilled = oldUnits + units;

                allocationListItem[Constants.LIST_ITEM_ALLOCATIONS_REMAINING] = newRemaining;
                allocationListItem[Constants.LIST_ITEM_ALLOCATIONS_FULFILLED] = fulfilled;

                allocationListItem.Update();
                clientContext.ExecuteQuery();
            }
        }

        private void updateArticlesListITem(ClientContext clientContext, SPRemoteEventProperties properties)
        {
            ListCollection webLists = clientContext.Web.Lists;
            List articlesList = webLists.GetByTitle(Constants.ARTICLES_LIBRARY_NAME);

            var articleId = Convert.ToString(properties.ItemEventProperties.AfterProperties[Constants.LIST_ITEM_ARTICLE_ID]);
            var oldArticleId = Convert.ToString(properties.ItemEventProperties.AfterProperties[Constants.INPUT_PREVIOUS_ARTICLE_ID]);
            var units = Convert.ToDecimal(properties.ItemEventProperties.AfterProperties[Constants.INPUT_UNIT]);

            var articleQuery = new CamlQuery();
            articleQuery.ViewXml = "<View><Query><Where><Geq><FieldRef Name='Article%5Fx0020%5FId'/>" +
                "<Value Type='Number'>" + articleId + "</Value></Geq></Where></Query><RowLimit>1</RowLimit></View>";
            var query = articlesList.GetItems(articleQuery);
            clientContext.Load(query);
            clientContext.ExecuteQuery();

            if (query.Count() > 0)
            {
                var articlesListItem = query.First();
                var oldUnits = Convert.ToDecimal(articlesListItem[Constants.LIST_ITEM_ALLOCATIONS_FULFILLED]);
                var fulfilled = oldUnits + units;

                articlesListItem[Constants.LIST_ITEM_ALLOCATIONS_FULFILLED] = fulfilled;

                articlesListItem.Update();
                clientContext.ExecuteQuery();
            }
        }
    }
}
