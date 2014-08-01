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

            var articleId = Convert.ToString(properties.ItemEventProperties.AfterProperties[Constants.LIST_ITEM_ARTICLE_ID]);

            var articleQuery = new CamlQuery();
            articleQuery.ViewXml = "<View><Query><Where><Geq><FieldRef Name='ArticleId'/>" +
                "<Value Type='Number'>" + articleId + "</Value></Geq></Where></Query><RowLimit>1</RowLimit></View>";

            //var q = allocationsList.GetItems(articleQuery).First();


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
