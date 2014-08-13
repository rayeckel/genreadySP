using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Microsoft.SharePoint.Client;
using Wictor.Office365;

namespace GRSPClassLibrary.Web
{
    public class DocumentUtils
    {
        public static void UploadFile(ClientContext clientContext, string listTitle, string sourceFileUrl, string libraryFileName)
        {
            var request = (HttpWebRequest)WebRequest.Create(sourceFileUrl);

            using(clientContext)
            using(var response = (HttpWebResponse) request.GetResponse())
            using(var receiveStream = (Stream) response.GetResponseStream())
            {
                //Load a refernce to the list
                var list = clientContext.Web.Lists.GetByTitle(listTitle);
                clientContext.Load(list.RootFolder);

                string claimSiteUrl = "https://generationreadydev.sharepoint.com/sites/re";
                string claimSiteUserName = "ray.eckel@generationreadydev.onmicrosoft.com";
                string claimSitePassword = "";

                var claimsHelper = new MsOnlineClaimsHelper(claimSiteUrl, claimSiteUserName, claimSitePassword);
                clientContext.ExecutingWebRequest += claimsHelper.clientContext_ExecutingWebRequest;
                clientContext.Load(clientContext.Web);

                clientContext.ExecuteQuery();

                Microsoft.SharePoint.Client.File.SaveBinaryDirect(clientContext, libraryFileName, receiveStream, true);
            }
        }

        public static void UploadDocument(ClientContext clientContext, string siteURL, string documentListName, string documentListURL, string documentName, byte[] documentStream)
        {
            using (clientContext)
            {
                //Get Document List
                List documentsList = clientContext.Web.Lists.GetByTitle(documentListName);

                var fileCreationInformation = new FileCreationInformation();
                //Assign to content byte[] i.e. documentStream

                fileCreationInformation.Content = documentStream;
                //Allow owerwrite of document

                fileCreationInformation.Overwrite = true;
                //Upload URL

                fileCreationInformation.Url = siteURL + documentListURL + documentName;
                Microsoft.SharePoint.Client.File uploadFile = documentsList.RootFolder.Files.Add(fileCreationInformation);

                //Update the metadata for a field having name "DocType"
                uploadFile.ListItemAllFields["DocType"] = "Favorites";

                uploadFile.ListItemAllFields.Update();
                clientContext.ExecuteQuery();

            }
        }
    }
}
