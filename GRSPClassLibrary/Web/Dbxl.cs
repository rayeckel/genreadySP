using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.SharePoint.Client;

namespace GRSPClassLibrary.Web.Dbxl
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
