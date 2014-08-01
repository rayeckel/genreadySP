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

            ListCollection webLists = clientContext.Web.Lists;
            //List updateFormsLibrary = webLists.GetById(listId);
            List allocationsList = webLists.GetByTitle(Constants.ALLOCATIONS_LIBRARY_NAME);
            List articlesList = webLists.GetByTitle(Constants.ARTICLES_LIBRARY_NAME);

            var allocationId = Convert.ToString(properties.ItemEventProperties.AfterProperties[Constants.LIST_ITEM_ALLOCATION_ID]);
            var articleId = Convert.ToString(properties.ItemEventProperties.AfterProperties[Constants.LIST_ITEM_ARTICLE_ID]);
            var oldAllocationId = Convert.ToString(properties.ItemEventProperties.AfterProperties[Constants.LIST_ITEM_ARTICLE_ID]);
            var oldArticleId = Convert.ToString(properties.ItemEventProperties.AfterProperties[Constants.LIST_ITEM_ARTICLE_ID]);
            var units = Convert.ToDecimal(properties.ItemEventProperties.AfterProperties[Constants.LIST_ITEM_ARTICLE_ID]);


            //Put the following in a separate function and call it from within the appropriate event handler below.

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

                //TODO: write new values to allocations table
            }


            var articleQuery = new CamlQuery();
            articleQuery.ViewXml = "<View><Query><Where><Geq><FieldRef Name='ArticleId'/>" +
                "<Value Type='Number'>" + articleId + "</Value></Geq></Where></Query><RowLimit>1</RowLimit></View>";
            query = allocationsList.GetItems(articleQuery);
            clientContext.Load(query);
            clientContext.ExecuteQuery();

            if (query.Count() > 0)
            {
                var articlesListItem = query.First();
                var oldUnits = Convert.ToDecimal(articlesListItem[Constants.LIST_ITEM_ALLOCATIONS_FULFILLED]);
                var fulfilled = oldUnits + units;

                //TODO: write new fulfilled values to articles table
            }







            switch (properties.EventType)
            {
                case SPRemoteEventType.ItemAdding:
                    {
                        try
                        {
                            syslogWriter.WriteLog("Fulfillment Tracking RER triggered", "Item Added");
                            result.ChangedItemProperties.Add(Constants.LIST_ITEM_ARTICLE_ID, "999999");
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
    }
}
