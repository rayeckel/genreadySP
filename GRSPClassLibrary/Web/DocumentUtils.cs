using System.IO;
using System.Security;
using System.Net;
using Microsoft.SharePoint.Client;
using GRSPClassLibrary.Base;

namespace GRSPClassLibrary.Web
{
    public class DocumentUtils
    {
        public static void UploadFile(ClientContext clientContext, string listTitle, string sourceFileUrl, string libraryFileName)
        {
            var request = (HttpWebRequest)WebRequest.Create(sourceFileUrl);

            using(clientContext)
            using(var response = (HttpWebResponse)request.GetResponse())
            using(var receiveStream = (Stream)response.GetResponseStream())
            {
                //Establish permission to upload to the list.
                clientContext.Credentials = 
                    new SharePointOnlineCredentials(Constants.CONTEXT_CREDENTIAL_USER_NAME, Constants.CONTEXT_CREDENTIAL_PASSWORD_SECURE);
                clientContext.Load(clientContext.Web);

                //Load a reference to the list
                var list = clientContext.Web.Lists.GetByTitle(listTitle);
                clientContext.Load(list.RootFolder);

                clientContext.ExecuteQuery();

                Microsoft.SharePoint.Client.File.SaveBinaryDirect(clientContext, libraryFileName, receiveStream, true);
            }
        }
    }
}
