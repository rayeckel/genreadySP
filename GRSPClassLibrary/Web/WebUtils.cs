using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Security;
using Newtonsoft.Json;
using Microsoft.SharePoint.Client;
using GRSPClassLibrary.Base;

namespace GRSPClassLibrary.Web
{
    public class WebUtils
    {
        private const string PUT = "PUT";
        private const string GET = "GET";

        public static void UploadFile(ClientContext clientContext, string listTitle, string sourceFileUrl, string libraryFileName, Dictionary<string, string> requestParams = null)
        {
            string securedUrl = sourceFileUrl;
            if (requestParams != null)
            {
                string requestHash = BuildPutRequestHash(requestParams);
                securedUrl = String.Format("{0}?{1}={2}", sourceFileUrl, Constants.UNSECURED_READY_PATH_URL_HASH_LABEL,  requestHash);
            }

            var request = CreateRequest(WebUtils.GET, new Uri(securedUrl));

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

        public static void PutData(string sourceFileUrl, Dictionary<string, string> requestParams = null)
        {
            if (requestParams != null)
            {
                var hash = BuildPutRequestHash(requestParams);
                requestParams.Add(Constants.UNSECURED_READY_PATH_URL_HASH_LABEL, hash);

                var request = CreateRequest(WebUtils.PUT, new Uri(sourceFileUrl));
                var json = JsonConvert.SerializeObject(requestParams);

                using (var writer = new StreamWriter(request.GetRequestStream()))
                {
                    writer.Write(json);
                }

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    response.GetType();
                }
            }
        }
        private static HttpWebRequest CreateRequest(string methodString, Uri addr, string contentType = "application/json")
        {
            var container = new CookieContainer();
            //container.Add(addr, new Cookie("session", Settings.AuthCookieValue));
            var request = (HttpWebRequest)WebRequest.Create(addr);
            request.Headers = new WebHeaderCollection();
            request.Accept = contentType;
            request.Method = methodString;
            request.CookieContainer = container;
            return request;
        }

        private static string BuildPutRequestHash(Dictionary<string, string> requestParams)
        {
            var sb = new StringBuilder(Crypt.Password);
            foreach (KeyValuePair<string, string> requestParam in requestParams)
            {
                sb.Append(requestParam.Value);
            }
            return Crypt.Encrypt(sb.ToString());
        }
    }
}
