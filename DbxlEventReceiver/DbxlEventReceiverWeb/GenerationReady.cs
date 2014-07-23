using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.SharePoint.Client;

namespace GenerationReady
{
    namespace Diagnostics
    {
        public class Log
        {
            public static void WriteLog(Web web, string Title, string Description)
            {
                List SystemLogList = web.Lists.GetByTitle("System Log");
                ListItemCreationInformation ListItemCreateInfo = new ListItemCreationInformation();
                ListItem SystemLogListItem = SystemLogList.AddItem(ListItemCreateInfo);
                SystemLogListItem["Title"] = Title;
                SystemLogListItem["Description"] = Description;
                SystemLogListItem.Update();
            }

            public static void WriteLog(Web web, string Title, string Description, string Detail)
            {
                List SystemLogList = web.Lists.GetByTitle("System Log");
                ListItemCreationInformation ListItemCreateInfo = new ListItemCreationInformation();
                ListItem SystemLogListItem = SystemLogList.AddItem(ListItemCreateInfo);
                SystemLogListItem["Title"] = Title;
                SystemLogListItem["Description"] = Description;
                SystemLogListItem["Detail"] = Detail;
                SystemLogListItem.Update();
            }
        }
    }

    namespace Dbxl
    {
        public class Properties
        {
            public static string GetDbxlProperty(string DbxlPropertyName, ClientContext clientContext)
            {
                clientContext.Load(clientContext.Web, web => web.AllProperties);
                clientContext.ExecuteQuery();
                if (clientContext.Web.AllProperties.FieldValues.ContainsKey(DbxlPropertyName))
                {
                    return clientContext.Web.AllProperties[DbxlPropertyName].ToString();
                }
                return null;
            }

            public static void SetDbxlProperty(string DbxlPropertyName, string DbxlProperty, ClientContext clientContext)
            {
                clientContext.Load(clientContext.Web, web => web.AllProperties);
                clientContext.ExecuteQuery();
                clientContext.Web.AllProperties[DbxlPropertyName] = DbxlProperty;
                clientContext.Web.Update();
                clientContext.ExecuteQuery();
            }
        }
    }
}