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
        private const string ALLOCATIONS_LIBRARY_NAME = "Allocations";
        private const string ALLOCATIONS_LIST_ITEM_ARTICLE_ID = "Article_x0020_Id";
        private const string ARTICLES_LIST_ITEM_ID = "ID";
        private const string ARTICLE_ID_LOOKUP_FIELD = "Article Id Lookup";
        private const string ARTICLES_LIBRARY_NAME = "Articles";
        private const string INPUT_UNIT = "Unit";
        private const string INPUT_SUBMITTED = "Submitted";
        private const string INPUT_PREVIOUS_SUBMITTED = "PreviousSubmitted";
        private const string LIST_ITEM_ALLOCATION_ID = "AllocationId";
        private const string LIST_ITEM_ARTICLE_ID = "ArticleId";
        private const string LIST_ITEM_ALLOCATIONS_QUANTITY = "Quantity";
        private const string LIST_ITEM_ALLOCATIONS_FULFILLED = "Fulfilled";
        private const string LIST_ITEM_ALLOCATIONS_REMAINING = "Remaining";
        protected override void ExecuteRER(SPRemoteEventProperties properties, ClientContext clientContext)
        {
            clientContext.Load(clientContext.Web, web => web.Lists);
            clientContext.ExecuteQuery();  

            switch (properties.EventType)
            {
                case SPRemoteEventType.ItemAdding:
                    {
                        try
                        {
                            UpdateAllocationsListItem(clientContext, properties);
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
                            UpdateAllocationsListItem(clientContext, properties);
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
                            DeleteAllocationsListItem(properties, clientContext);
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

        private void UpdateAllocationsListItem(ClientContext clientContext, SPRemoteEventProperties properties)
        {
            string submittedDate = Convert.ToString(properties.ItemEventProperties.AfterProperties[FulfillmentTrackingRER.INPUT_SUBMITTED]);

            //If the form is not marked as submitted, skip the calculations to Articles and Allocations
            if (String.IsNullOrEmpty(submittedDate))
            {
                return;
            }

            string allocationId = Convert.ToString(properties.ItemEventProperties.AfterProperties[FulfillmentTrackingRER.LIST_ITEM_ALLOCATION_ID]);
            decimal units = Convert.ToDecimal(properties.ItemEventProperties.AfterProperties[FulfillmentTrackingRER.INPUT_UNIT]);

            ListItem itemUpdating = GetFormsListItem(properties, clientContext);

            //If the form was previously submitted, and is now being modified.
            if (itemUpdating != null)
            {
                decimal previousUnits = Convert.ToDecimal(itemUpdating[FulfillmentTrackingRER.INPUT_UNIT]);

                //If the allocation ID is changing, reverse the updates to the old allocation and its related article.
                string oldAllocationId = Convert.ToString(itemUpdating[FulfillmentTrackingRER.LIST_ITEM_ALLOCATION_ID]);
                if (allocationId != oldAllocationId)
                {
                    try
                    {
                        //Remove previous record
                        RemoveDays(properties, clientContext, oldAllocationId, previousUnits);
                        //Update to new Allocation Id
                        AddDays(properties, clientContext, allocationId, units);
                    }
                    catch (Exception ex)
                    {
                        errorlogWriter.WriteLog("Fulfillment Tracking RER Item UPDATNG ERROR", ex.Message);
                    }
                }

                //If modifying the amount of units reported, adjust the calculations
                if(units != previousUnits)
                {
                    decimal diff = units - previousUnits;
                    try
                    {
                        AddDays(properties, clientContext, allocationId, diff);
                    }
                    catch (Exception ex)
                    {
                        errorlogWriter.WriteLog("Fulfillment Tracking RER Item UPDATNG ERROR", ex.Message);
                    }
                }
            }
            //If the form is being submitted for the first time, do the calculations
            else
            {
                try
                {
                    int articleId = AddDays(properties, clientContext, allocationId, units);

                    //Add the fieldLookupValue
                    result.ChangedItemProperties.Add(FulfillmentTrackingRER.ARTICLE_ID_LOOKUP_FIELD, articleId);

                    //Mark the record as having already been submitted.
                    result.ChangedItemProperties.Add(FulfillmentTrackingRER.INPUT_PREVIOUS_SUBMITTED, submittedDate);
                }
                catch (Exception ex)
                {
                    errorlogWriter.WriteLog("Fulfillment Tracking RER Item ADDING ERROR", ex.Message);
                }
            }
        }

        private void DeleteAllocationsListItem(SPRemoteEventProperties properties, ClientContext clientContext)
        {
            ListItem itemDeleting = GetFormsListItem(properties, clientContext);
            string allocationId = Convert.ToString(itemDeleting[FulfillmentTrackingRER.LIST_ITEM_ALLOCATION_ID]);
            decimal units = Convert.ToDecimal(itemDeleting[FulfillmentTrackingRER.INPUT_UNIT]);

            RemoveDays(properties, clientContext, allocationId, units);
        }

        private ListItem GetFormsListItem(SPRemoteEventProperties properties, ClientContext clientContext)
        {
            ListCollection webLists = clientContext.Web.Lists;
            List formsList = webLists.GetByTitle(Constants.TRACKING_LIBRARY_NAME);
            string itemId = Convert.ToString(properties.ItemEventProperties.ListItemId);

            var formsQuery = new CamlQuery();
            formsQuery.ViewXml = "<View><Query><Where><Eq><FieldRef Name='ID'/>" +
                "<Value Type='Number'>" + itemId + "</Value></Eq></Where></Query></View>";
            ListItemCollection query = formsList.GetItems(formsQuery);

            clientContext.Load(query);
            clientContext.ExecuteQuery();

            if (query.Count() > 0)
            {
                return query.First();
            }

            return null;
        }

        private ListItem GetAllocationsListItem(SPRemoteEventProperties properties, ClientContext clientContext, string allocationId)
        {
            ListCollection webLists = clientContext.Web.Lists;
            List allocationsList = webLists.GetByTitle(FulfillmentTrackingRER.ALLOCATIONS_LIBRARY_NAME);

            var allocationQuery = new CamlQuery();
            allocationQuery.ViewXml = "<View><Query><Where><Eq><FieldRef Name='ID'/>" +
                "<Value Type='Number'>" + allocationId + "</Value></Eq></Where></Query></View>";
            ListItemCollection query = allocationsList.GetItems(allocationQuery);

            clientContext.Load(query);
            clientContext.ExecuteQuery();

            if (query.Count() > 0)
            {
                return query.First();
            }

            return null;
        }

        private ListItem GetArticlesListItem(SPRemoteEventProperties properties, ClientContext clientContext, string articleId)
        {
            ListCollection webLists = clientContext.Web.Lists;
            List articlesList = webLists.GetByTitle(FulfillmentTrackingRER.ARTICLES_LIBRARY_NAME);

            var articleQuery = new CamlQuery();
            articleQuery.ViewXml = "<View><Query><Where><Eq><FieldRef Name='Article_x0020_Id'/>" +
                "<Value Type='Text'>" + articleId + "</Value></Eq></Where></Query></View>";
            ListItemCollection query = articlesList.GetItems(articleQuery);
            clientContext.Load(query);
            clientContext.ExecuteQuery();

            if (query.Count() > 0)
            {
                return query.First();
            }

            return null;
        }

        private int AddDays(SPRemoteEventProperties properties, ClientContext clientContext, string allocationId, decimal units)
        {
            ListItem allocationListItem = GetAllocationsListItem(properties, clientContext, allocationId);
            IncrementAllocationsFulfilled(allocationListItem, units);
            clientContext.ExecuteQuery();

            string articleId = Convert.ToString(allocationListItem[FulfillmentTrackingRER.ALLOCATIONS_LIST_ITEM_ARTICLE_ID]);
            ListItem articlesListItem = GetArticlesListItem(properties, clientContext, articleId);
            IncrementArticlesFulfilled(articlesListItem, units);
            clientContext.ExecuteQuery();

            return (int)articlesListItem[FulfillmentTrackingRER.ARTICLES_LIST_ITEM_ID];
        }

        private void RemoveDays(SPRemoteEventProperties properties, ClientContext clientContext, string allocationId, decimal units)
        {
            ListItem oldAllocationListItem = GetAllocationsListItem(properties, clientContext, allocationId);
            DecrementAllocationsFulfilled(ref oldAllocationListItem, units);
            clientContext.ExecuteQuery();

            string oldAllocationArticleId = Convert.ToString(oldAllocationListItem[FulfillmentTrackingRER.ALLOCATIONS_LIST_ITEM_ARTICLE_ID]);
            ListItem oldAllocationArticleListItem = GetArticlesListItem(properties, clientContext, oldAllocationArticleId);
            DecrementArticlesFulfilled(ref oldAllocationArticleListItem, units);
            clientContext.ExecuteQuery();
        }

        private void IncrementAllocationsFulfilled(ListItem allocationListItem, Decimal units)
        {
            if (allocationListItem != null)
            {
                decimal oldUnits = Convert.ToDecimal(allocationListItem[FulfillmentTrackingRER.LIST_ITEM_ALLOCATIONS_FULFILLED]);
                decimal oldRemaining = Convert.ToDecimal(allocationListItem[FulfillmentTrackingRER.LIST_ITEM_ALLOCATIONS_REMAINING]);

                decimal newRemaining = oldRemaining - units;
                decimal fulfilled = oldUnits + units;

                allocationListItem[FulfillmentTrackingRER.LIST_ITEM_ALLOCATIONS_REMAINING] = newRemaining;
                allocationListItem[FulfillmentTrackingRER.LIST_ITEM_ALLOCATIONS_FULFILLED] = fulfilled;
                allocationListItem.Update();
            }
            else
            {
                errorlogWriter.WriteLog("WARNING: Fulfillment Tracking RER Item ADDING", "Allocations list item not found. Not INCREMENTED");
            }
        }

        private void DecrementAllocationsFulfilled(ref ListItem allocationListItem, Decimal units)
        {
            if (allocationListItem != null)
            {
                decimal oldUnits = Convert.ToDecimal(allocationListItem[FulfillmentTrackingRER.LIST_ITEM_ALLOCATIONS_FULFILLED]);
                decimal oldRemaining = Convert.ToDecimal(allocationListItem[FulfillmentTrackingRER.LIST_ITEM_ALLOCATIONS_REMAINING]);

                decimal newRemaining = oldRemaining + units;
                decimal fulfilled = oldUnits - units;

                allocationListItem[FulfillmentTrackingRER.LIST_ITEM_ALLOCATIONS_REMAINING] = newRemaining;
                allocationListItem[FulfillmentTrackingRER.LIST_ITEM_ALLOCATIONS_FULFILLED] = fulfilled;
                allocationListItem.Update();
            }
            else
            {
                errorlogWriter.WriteLog("WARNING: Fulfillment Tracking RER Item ADDING", "Allocations list item not found. Not DECREMENTED");
            }
        }

        private void IncrementArticlesFulfilled(ListItem articlesListItem, Decimal units)
        {
            if (articlesListItem != null)
            {
                decimal oldUnits = Convert.ToDecimal(articlesListItem[FulfillmentTrackingRER.LIST_ITEM_ALLOCATIONS_FULFILLED]);
                decimal fulfilled = oldUnits + units;

                articlesListItem[FulfillmentTrackingRER.LIST_ITEM_ALLOCATIONS_FULFILLED] = fulfilled;
                articlesListItem.Update();
            }
            else
            {
                errorlogWriter.WriteLog("WARNING: Fulfillment Tracking RER Item ADDING", "Articles list item not found. Not INCREMENTED");
            }
        }

        private void DecrementArticlesFulfilled(ref ListItem articlesListItem, Decimal units)
        {
            if (articlesListItem != null)
            {
                decimal oldUnits = Convert.ToDecimal(articlesListItem[FulfillmentTrackingRER.LIST_ITEM_ALLOCATIONS_FULFILLED]);
                decimal fulfilled = oldUnits - units;

                articlesListItem[FulfillmentTrackingRER.LIST_ITEM_ALLOCATIONS_FULFILLED] = fulfilled;
                articlesListItem.Update();
            }
            else
            {
                errorlogWriter.WriteLog("WARNING: Fulfillment Tracking RER Item ADDING", "Articles list item not found. Not DECREMENTED");
            }
        }
    }
}
