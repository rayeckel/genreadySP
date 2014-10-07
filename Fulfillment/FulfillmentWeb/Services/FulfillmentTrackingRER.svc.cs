using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.EventReceivers;
using FulfillmentWeb.Base;
using GRSPClassLibrary.Web;
using System.ServiceModel;

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
        private const string INPUT_SUBMITTED = "Submitted0";
        private const string INPUT_PREVIOUS_SUBMITTED = "PreviousSubmitted";
        private const string LIST_ITEM_ALLOCATION_ID = "AllocationId";
        private const string LIST_ITEM_ARTICLE_ID = "ArticleId";
        private const string LIST_ITEM_ALLOCATIONS_QUANTITY = "Quantity";
        private const string LIST_ITEM_ALLOCATIONS_FULFILLED = "Fulfilled";
        private const string LIST_ITEM_ALLOCATIONS_REMAINING = "Remaining";
        protected override void ExecuteRER(SPRemoteEventProperties properties, ClientContext clientContext)
        {
            syslogWriter.WriteLog("Fulfillment Tracking RER  DEBUG", "LOAD CLIENT CONTEXT WEB", properties.ItemEventProperties.ListItemId);

            clientContext.Load(clientContext.Web, web => web.Lists);
            ExecuteQuery(clientContext, properties.ItemEventProperties.ListItemId);






            //ListCollection webLists = clientContext.Web.Lists;
            //List trackingList = webLists.GetByTitle("Project Update Forms");

            //EventReceiverDefinitionCollection erdCollection = trackingList.EventReceivers;
            //clientContext.Load(erdCollection);
            //clientContext.ExecuteQuery();

            //foreach (EventReceiverDefinition erd in erdCollection)
            //{
            //    if (erd.ReceiverName == "FulfillmentTrackingRER")
            //    {
            //        erd.DeleteObject();
            //    }
            //}



            //string opContext = OperationContext.Current.Channel.LocalAddress.Uri.AbsoluteUri.Substring(0, OperationContext.Current.Channel.LocalAddress.Uri.AbsoluteUri.LastIndexOf("/"));
            //string remoteUrl = string.Format("{0}/{1}.svc", opContext, "FulfillmentTrackingRER");
            //foreach (var receiverType in eventReceiverTypes)
            //{
            //    trackingList.EventReceivers.Add(new EventReceiverDefinitionCreationInformation()
            //    {
            //        EventType = receiverType,
            //        ReceiverName = "FulfillmentTrackingRER",
            //        ReceiverUrl = remoteUrl,
            //        SequenceNumber = 1000
            //    });
            //}

            //clientContext.ExecuteQuery();




            syslogWriter.WriteLog("Fulfillment Tracking RER  DEBUG", "PRE SWITCHCASE", properties.ItemEventProperties.ListItemId);
            switch (properties.EventType)
            {
                case SPRemoteEventType.ItemAdding:
                    {
                        try
                        {
                            syslogWriter.WriteLog("Fulfillment Tracking RER  DEBUG", "ITEM ADDING", properties.ItemEventProperties.ListItemId);
                            UpdateAllocationsListItem(clientContext, properties);
                            syslogWriter.WriteLog("Fulfillment Tracking RER triggered", "Item Added", properties.ItemEventProperties.ListItemId);
                        }
                        catch (Exception ex)
                        {
                            errorlogWriter.WriteLog("Fulfillment Tracking RER Item Added ERROR", ex.Message, properties.ItemEventProperties.ListItemId);
                        }

                        break;
                    }
                case SPRemoteEventType.ItemUpdating:
                    {
                        try
                        {
                            syslogWriter.WriteLog("Fulfillment Tracking RER  DEBUG", "ITEM UPDATING", properties.ItemEventProperties.ListItemId);
                            UpdateAllocationsListItem(clientContext, properties);
                            syslogWriter.WriteLog("Fulfillment Tracking RER  triggered", "Item Updated", properties.ItemEventProperties.ListItemId);
                        }
                        catch (Exception ex)
                        {
                            errorlogWriter.WriteLog("Fulfillment Tracking RER Item Updated triggered", ex.Message, properties.ItemEventProperties.ListItemId);
                        }

                        break;
                    }
                case SPRemoteEventType.ItemDeleting:
                    {
                        try
                        {
                            DeleteAllocationsListItem(properties, clientContext);
                            syslogWriter.WriteLog("Fulfillment Tracking RER  triggered", "Item Deleting", properties.ItemEventProperties.ListItemId);
                        }
                        catch (Exception ex)
                        {
                            errorlogWriter.WriteLog("Fulfillment Tracking RER Item Deleting ERROR", ex.Message, properties.ItemEventProperties.ListItemId);
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
            if (properties.ItemEventProperties == null)
            {
                syslogWriter.WriteLog("Fulfillment Tracking RER DEBUG", "ItemEvent Properties is NULL", properties.ItemEventProperties.ListItemId);
            }
            if (properties.ItemEventProperties.BeforeProperties == null)
            {
                syslogWriter.WriteLog("Fulfillment Tracking RER DEBUG", "BeforeProperties is NULL", properties.ItemEventProperties.ListItemId);
            }
            if (properties.ItemEventProperties.AfterProperties == null)
            {
                syslogWriter.WriteLog("Fulfillment Tracking RER DEBUG", "AfterProperties is NULL", properties.ItemEventProperties.ListItemId);
            }

            //If the DbxlId value was previously null, but AfterProperties now has a value, return.
            if (!properties.ItemEventProperties.BeforeProperties.ContainsKey(GRSPClassLibrary.Base.Constants.DBXL_ID_LABEL) &&
                properties.ItemEventProperties.AfterProperties.ContainsKey(GRSPClassLibrary.Base.Constants.DBXL_ID_LABEL))
            {
                return;
            }

            //If the form is not marked as submitted, skip the calculations to Articles and Allocations
            var submittedDate = String.Empty;
            if (!properties.ItemEventProperties.AfterProperties.ContainsKey(FulfillmentTrackingRER.INPUT_SUBMITTED))
            {
                return;
            }
            else 
            {
                syslogWriter.WriteLog("Fulfillment Tracking RER DEBUG", "PRE-SUBMITTED0", properties.ItemEventProperties.ListItemId);
                var submitDate = properties.ItemEventProperties.AfterProperties[FulfillmentTrackingRER.INPUT_SUBMITTED];
                if (submitDate != null)
                {
                    submittedDate = submitDate.ToString();
                    syslogWriter.WriteLog("Fulfillment Tracking RER DEBUG", "POST-SUBMITTED0", properties.ItemEventProperties.ListItemId);
                }
                else
                {
                    syslogWriter.WriteLog("Fulfillment Tracking RER DEBUG", "POST-SUBMITTED0", properties.ItemEventProperties.ListItemId);
                    return;
                }
            }

            string allocationId = Convert.ToString(properties.ItemEventProperties.AfterProperties[FulfillmentTrackingRER.LIST_ITEM_ALLOCATION_ID]);
            decimal units = Convert.ToDecimal(properties.ItemEventProperties.AfterProperties[FulfillmentTrackingRER.INPUT_UNIT]);

            syslogWriter.WriteLog("Fulfillment Tracking RER DEBUG", "PRE-GetFormsListItem", properties.ItemEventProperties.ListItemId);

            ListItem itemUpdating = GetFormsListItem(properties, clientContext);

            syslogWriter.WriteLog("Fulfillment Tracking RER DEBUG", "POST-GetFormsListItem", properties.ItemEventProperties.ListItemId);
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
                        int articleId = AddDays(properties, clientContext, allocationId, units);
                        //Update the ArticleId Lookup Field
                        result.ChangedItemProperties.Add(FulfillmentTrackingRER.ARTICLE_ID_LOOKUP_FIELD, articleId);
                    }
                    catch (Exception ex)
                    {
                        errorlogWriter.WriteLog("Fulfillment Tracking RER Item UPDATNG ERROR", ex.Message, properties.ItemEventProperties.ListItemId);
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
                        errorlogWriter.WriteLog("Fulfillment Tracking RER Item UPDATNG ERROR", ex.Message, properties.ItemEventProperties.ListItemId);
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
                    //result.ChangedItemProperties.Add(FulfillmentTrackingRER.ARTICLE_ID_LOOKUP_FIELD, articleId);

                    //Mark the record as having already been submitted.
                    //result.ChangedItemProperties.Add(FulfillmentTrackingRER.INPUT_PREVIOUS_SUBMITTED, submittedDate);
                }
                catch (Exception ex)
                {
                    errorlogWriter.WriteLog("Fulfillment Tracking RER Item ADDING ERROR", ex.Message, properties.ItemEventProperties.ListItemId);
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

            syslogWriter.WriteLog("Fulfillment Tracking RER DEBUG", "INSIDE-GetFormsListItem", properties.ItemEventProperties.ListItemId);

            var formsQuery = new CamlQuery();
            formsQuery.ViewXml = "<View><Query><Where><Eq><FieldRef Name='ID'/>" +
                "<Value Type='Number'>" + itemId + "</Value></Eq></Where></Query></View>";
            ListItemCollection query = formsList.GetItems(formsQuery);

            clientContext.Load(query);
            ExecuteQuery(clientContext, properties.ItemEventProperties.ListItemId);

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
            ExecuteQuery(clientContext);

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

            ExecuteQuery(clientContext);

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

            ExecuteQuery(clientContext);

            string articleId = Convert.ToString(allocationListItem[FulfillmentTrackingRER.ALLOCATIONS_LIST_ITEM_ARTICLE_ID]);

            ListItem articlesListItem = GetArticlesListItem(properties, clientContext, articleId);
            IncrementArticlesFulfilled(articlesListItem, units);

            ExecuteQuery(clientContext);

            return (int)articlesListItem[FulfillmentTrackingRER.ARTICLES_LIST_ITEM_ID];
        }

        private void RemoveDays(SPRemoteEventProperties properties, ClientContext clientContext, string allocationId, decimal units)
        {
            ListItem oldAllocationListItem = GetAllocationsListItem(properties, clientContext, allocationId);
            DecrementAllocationsFulfilled(ref oldAllocationListItem, units);
            ExecuteQuery(clientContext);

            string oldAllocationArticleId = Convert.ToString(oldAllocationListItem[FulfillmentTrackingRER.ALLOCATIONS_LIST_ITEM_ARTICLE_ID]);
            ListItem oldAllocationArticleListItem = GetArticlesListItem(properties, clientContext, oldAllocationArticleId);
            DecrementArticlesFulfilled(ref oldAllocationArticleListItem, units);
            ExecuteQuery(clientContext);
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
