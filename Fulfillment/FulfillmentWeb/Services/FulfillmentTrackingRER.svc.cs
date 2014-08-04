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
                            DeleteAllocationsListItem(clientContext, properties);
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
            string submittedDate = Convert.ToString(properties.ItemEventProperties.AfterProperties[Constants.INPUT_SUBMITTED]);

            //If the form is not marked as submitted, skip the calculations to Articles and Allocations
            if (String.IsNullOrEmpty(submittedDate))
            {
                return;
            }

            string allocationId = Convert.ToString(properties.ItemEventProperties.AfterProperties[Constants.LIST_ITEM_ALLOCATION_ID]);
            decimal units = Convert.ToDecimal(properties.ItemEventProperties.AfterProperties[Constants.INPUT_UNIT]);
            string previousSubmittedDate = Convert.ToString(properties.ItemEventProperties.AfterProperties[Constants.INPUT_PREVIOUS_SUBMITTED]);

            //If the form was previously submitted, and is now being modified.
            if(!String.IsNullOrEmpty(previousSubmittedDate))
            {
                decimal previousUnits = Convert.ToDecimal(properties.ItemEventProperties.BeforeProperties[Constants.INPUT_UNIT]);

                //If the allocation ID is changing, reverse the updates to the old allocation and its related article.
                string oldAllocationId = Convert.ToString(properties.ItemEventProperties.AfterProperties[Constants.INPUT_PREVIOUS_ALLOCATION_ID]);
                if (allocationId != oldAllocationId)
                {
                    RemoveDays(properties, clientContext, oldAllocationId, previousUnits);
                }

                //If modifying the amount of units reported, adjust the calculations
                if(units != previousUnits)
                {
                    decimal diff = units - previousUnits;
                    AddDays(properties, clientContext, allocationId, diff);
                }
            }
            //If the form is being submitted for the first time, do the calculations
            else
            {
                AddDays(properties, clientContext, allocationId, units);

                //Mark the record as having already been submitted.
                result.ChangedItemProperties.Add(Constants.INPUT_PREVIOUS_SUBMITTED, submittedDate);
            }
        }

        private void DeleteAllocationsListItem(ClientContext clientContext, SPRemoteEventProperties properties)
        {
            string allocationId = Convert.ToString(properties.ItemEventProperties.AfterProperties[Constants.LIST_ITEM_ALLOCATION_ID]);
            decimal units = Convert.ToDecimal(properties.ItemEventProperties.AfterProperties[Constants.INPUT_UNIT]);

            RemoveDays(properties, clientContext, allocationId, units);
        }

        private ListItem GetAllocationsListItem(SPRemoteEventProperties properties, ClientContext clientContext, string allocationId)
        {
            ListCollection webLists = clientContext.Web.Lists;
            List allocationsList = webLists.GetByTitle(Constants.ALLOCATIONS_LIBRARY_NAME);

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
            List articlesList = webLists.GetByTitle(Constants.ARTICLES_LIBRARY_NAME);

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

        private void AddDays(SPRemoteEventProperties properties, ClientContext clientContext, string allocationId, decimal units)
        {
            ListItem allocationListItem = GetAllocationsListItem(properties, clientContext, allocationId);
            IncrementAllocationsFulfilled(allocationListItem, units);
            clientContext.ExecuteQuery();

            string articleId = Convert.ToString(allocationListItem[Constants.ALLOCATIONS_LIST_ITEM_ARTICLE_ID]);
            ListItem articlesListItem = GetArticlesListItem(properties, clientContext, articleId);
            IncrementArticlesFulfilled(articlesListItem, units);
            clientContext.ExecuteQuery();
        }

        private void RemoveDays(SPRemoteEventProperties properties, ClientContext clientContext, string allocationId, decimal units)
        {
            ListItem oldAllocationListItem = GetAllocationsListItem(properties, clientContext, allocationId);
            DecrementAllocationsFulfilled(oldAllocationListItem, units);
            clientContext.ExecuteQuery();

            string oldAllocationArticleId = Convert.ToString(oldAllocationListItem[Constants.ALLOCATIONS_LIST_ITEM_ARTICLE_ID]);
            ListItem oldAllocationArticleListItem = GetArticlesListItem(properties, clientContext, oldAllocationArticleId);
            DecrementArticlesFulfilled(oldAllocationArticleListItem, units);
            clientContext.ExecuteQuery();
        }

        private void IncrementAllocationsFulfilled(ListItem allocationListItem, Decimal units)
        {
            if (allocationListItem != null)
            {
                decimal oldUnits = Convert.ToDecimal(allocationListItem[Constants.LIST_ITEM_ALLOCATIONS_FULFILLED]);
                decimal oldRemaining = Convert.ToDecimal(allocationListItem[Constants.LIST_ITEM_ALLOCATIONS_REMAINING]);

                decimal newRemaining = oldRemaining - units;
                decimal fulfilled = oldUnits + units;

                allocationListItem[Constants.LIST_ITEM_ALLOCATIONS_REMAINING] = newRemaining;
                allocationListItem[Constants.LIST_ITEM_ALLOCATIONS_FULFILLED] = fulfilled;
                allocationListItem.Update();
            }
        }

        private void DecrementAllocationsFulfilled(ListItem allocationListItem, Decimal units)
        {
            if (allocationListItem != null)
            {
                decimal oldUnits = Convert.ToDecimal(allocationListItem[Constants.LIST_ITEM_ALLOCATIONS_FULFILLED]);
                decimal oldRemaining = Convert.ToDecimal(allocationListItem[Constants.LIST_ITEM_ALLOCATIONS_REMAINING]);

                decimal newRemaining = oldRemaining + units;
                decimal fulfilled = oldUnits - units;

                allocationListItem[Constants.LIST_ITEM_ALLOCATIONS_REMAINING] = newRemaining;
                allocationListItem[Constants.LIST_ITEM_ALLOCATIONS_FULFILLED] = fulfilled;
                allocationListItem.Update();
            }
        }

        private void IncrementArticlesFulfilled(ListItem articlesListItem, Decimal units)
        {
            if (articlesListItem != null)
            {
                decimal oldUnits = Convert.ToDecimal(articlesListItem[Constants.LIST_ITEM_ALLOCATIONS_FULFILLED]);
                decimal fulfilled = oldUnits + units;

                articlesListItem[Constants.LIST_ITEM_ALLOCATIONS_FULFILLED] = fulfilled;
                articlesListItem.Update();
            }
        }

        private void DecrementArticlesFulfilled(ListItem articlesListItem, Decimal units)
        {
            if (articlesListItem != null)
            {
                decimal oldUnits = Convert.ToDecimal(articlesListItem[Constants.LIST_ITEM_ALLOCATIONS_FULFILLED]);
                decimal fulfilled = oldUnits - units;

                articlesListItem[Constants.LIST_ITEM_ALLOCATIONS_FULFILLED] = fulfilled;
                articlesListItem.Update();
            }
        }
    }
}
