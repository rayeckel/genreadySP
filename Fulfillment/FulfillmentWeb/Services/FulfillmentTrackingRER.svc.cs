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

            updateAllocationsListItem(clientContext, properties);
            updateArticlesListItem(clientContext, properties);

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
        }

        private void updateAllocationsListItem(ClientContext clientContext, SPRemoteEventProperties properties)
        {
            var allocationId = Convert.ToString(properties.ItemEventProperties.AfterProperties[Constants.LIST_ITEM_ALLOCATION_ID]);
            var units = Convert.ToDecimal(properties.ItemEventProperties.AfterProperties[Constants.INPUT_UNIT]);

            var allocationListItem = getAllocationsListItem(properties, clientContext, allocationId);
            incrementAllocationsFulfilled(allocationListItem, units);

            //If the allocation ID is changing, reverse the updates to the old allocation.
            var oldAllocationId = Convert.ToString(properties.ItemEventProperties.AfterProperties[Constants.INPUT_PREVIOUS_ALLOCATION_ID]);
            if (allocationId != oldAllocationId)
            {
                var oldAllocationListItem = getAllocationsListItem(properties, clientContext, oldAllocationId);
                decrementAllocationsFulfilled(oldAllocationListItem, units);

                //Decrement the article so that when 'updateArticleListItem' is called,
                //It does not result in too much being added.
                var articleId = Convert.ToString(properties.ItemEventProperties.AfterProperties[Constants.LIST_ITEM_ARTICLE_ID]);
                var oldArticlesListItem = getArticlesListItem(properties, clientContext, articleId);
                decrementArticlesFulfilled(oldArticlesListItem, units);
            }

            clientContext.ExecuteQuery();
        }

        private void updateArticlesListItem(ClientContext clientContext, SPRemoteEventProperties properties)
        {
            var articleId = Convert.ToString(properties.ItemEventProperties.AfterProperties[Constants.LIST_ITEM_ARTICLE_ID]);
            var units = Convert.ToDecimal(properties.ItemEventProperties.AfterProperties[Constants.INPUT_UNIT]);

            var articlesListItem = getArticlesListItem(properties, clientContext, articleId);
            incrementArticlesFulfilled(articlesListItem, units);

            //If the article ID is changing, reverse the updates to the old article.
            var oldArticleId = Convert.ToString(properties.ItemEventProperties.AfterProperties[Constants.INPUT_PREVIOUS_ARTICLE_ID]);
            if (articleId != oldArticleId)
            {
                var oldArticlesListItem = getArticlesListItem(properties, clientContext, oldArticleId);
                decrementArticlesFulfilled(oldArticlesListItem, units);

                //Decrement the allocation to offset the previous call to 'updateAllocationListItem'
                var allocationId = Convert.ToString(properties.ItemEventProperties.AfterProperties[Constants.LIST_ITEM_ALLOCATION_ID]);
                var allocationListItem = getAllocationsListItem(properties, clientContext, allocationId);
                decrementAllocationsFulfilled(allocationListItem, units);
            }

            clientContext.ExecuteQuery();
        }

        private ListItem getAllocationsListItem(SPRemoteEventProperties properties, ClientContext clientContext, string allocationId)
        {
            ListCollection webLists = clientContext.Web.Lists;
            List allocationsList = webLists.GetByTitle(Constants.ALLOCATIONS_LIBRARY_NAME);

            var allocationQuery = new CamlQuery();
            allocationQuery.ViewXml = "<View><Query><Where><Eq><FieldRef Name='ID'/>" +
                "<Value Type='Number'>" + allocationId + "</Value></Eq></Where></Query></View>";
            var query = allocationsList.GetItems(allocationQuery);

            clientContext.Load(query);
            clientContext.ExecuteQuery();

            if (query.Count() > 0)
            {
                return query.First();
            }

            return null;
        }

        private ListItem getArticlesListItem(SPRemoteEventProperties properties, ClientContext clientContext, string articleId)
        {
            ListCollection webLists = clientContext.Web.Lists;
            List articlesList = webLists.GetByTitle(Constants.ARTICLES_LIBRARY_NAME);

            var articleQuery = new CamlQuery();
            articleQuery.ViewXml = "<View><Query><Where><Eq><FieldRef Name='Article_x0020_Id'/>" +
                "<Value Type='Text'>" + articleId + "</Value></Eq></Where></Query></View>";
            var query = articlesList.GetItems(articleQuery);
            clientContext.Load(query);
            clientContext.ExecuteQuery();

            if (query.Count() > 0)
            {
                return query.First();
            }

            return null;
        }

        private void incrementAllocationsFulfilled(ListItem allocationListItem, Decimal units)
        {
            if (allocationListItem != null)
            {
                var oldUnits = Convert.ToDecimal(allocationListItem[Constants.LIST_ITEM_ALLOCATIONS_FULFILLED]);
                var oldRemaining = Convert.ToDecimal(allocationListItem[Constants.LIST_ITEM_ALLOCATIONS_REMAINING]);

                var newRemaining = oldRemaining - units;
                var fulfilled = oldUnits + units;

                allocationListItem[Constants.LIST_ITEM_ALLOCATIONS_REMAINING] = newRemaining;
                allocationListItem[Constants.LIST_ITEM_ALLOCATIONS_FULFILLED] = fulfilled;
                allocationListItem.Update();
            }
        }

        private void decrementAllocationsFulfilled(ListItem allocationListItem, Decimal units)
        {
            if (allocationListItem != null)
            {
                var oldUnits = Convert.ToDecimal(allocationListItem[Constants.LIST_ITEM_ALLOCATIONS_FULFILLED]);
                var oldRemaining = Convert.ToDecimal(allocationListItem[Constants.LIST_ITEM_ALLOCATIONS_REMAINING]);

                var newRemaining = oldRemaining + units;
                var fulfilled = oldUnits - units;

                allocationListItem[Constants.LIST_ITEM_ALLOCATIONS_REMAINING] = newRemaining;
                allocationListItem[Constants.LIST_ITEM_ALLOCATIONS_FULFILLED] = fulfilled;
                allocationListItem.Update();
            }
        }

        private void incrementArticlesFulfilled(ListItem articlesListItem, Decimal units)
        {
            if (articlesListItem != null)
            {
                var oldUnits = Convert.ToDecimal(articlesListItem[Constants.LIST_ITEM_ALLOCATIONS_FULFILLED]);
                var fulfilled = oldUnits + units;

                articlesListItem[Constants.LIST_ITEM_ALLOCATIONS_FULFILLED] = fulfilled;
                articlesListItem.Update();
            }
        }

        private void decrementArticlesFulfilled(ListItem articlesListItem, Decimal units)
        {
            if (articlesListItem != null)
            {
                var oldUnits = Convert.ToDecimal(articlesListItem[Constants.LIST_ITEM_ALLOCATIONS_FULFILLED]);
                var fulfilled = oldUnits - units;

                articlesListItem[Constants.LIST_ITEM_ALLOCATIONS_FULFILLED] = fulfilled;
                articlesListItem.Update();
            }
        }
    }
}
