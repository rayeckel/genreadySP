using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                //Pass params Dictionary by reference since BuildPutRequestHash() adds hash and nonce
                BuildPutRequestHash(ref requestParams);
                securedUrl = String.Format("{0}?{1}={2}", sourceFileUrl, Constants.UNSECURED_READY_PATH_URL_HASH_LABEL, requestParams[Constants.UNSECURED_READY_PATH_URL_HASH_LABEL]);
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

                //Upload the file to the Document Library
                Microsoft.SharePoint.Client.File.SaveBinaryDirect(clientContext, libraryFileName, receiveStream, true);
            }
        }

        public static void PutData(string sourceFileUrl, Dictionary<string, string> requestParams = null)
        {
            if (requestParams != null)
            {
                //Pass params Dictionary by reference since BuildPutRequestHash() adds hash and nonce
                BuildPutRequestHash(ref requestParams);
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
            var request = (HttpWebRequest)WebRequest.Create(addr);
            //var container = new CookieContainer();
            //container.Add(addr, new Cookie("session", Settings.AuthCookieValue));

            //request.CookieContainer = container;
            request.Headers = new WebHeaderCollection();
            request.Accept = contentType;
            request.Method = methodString;

            return request;
        }

        private static void BuildPutRequestHash(ref Dictionary<string, string> requestParams)
        {
            //Genrate a nonce by appending a random number to the string representation of now().
            var rnd = new Random();
            string randomNumber = rnd.Next(10000, 99999).ToString();
            string now = DateTime.Now.ToString();
            string nonce = String.Format("{0}{1}", now, randomNumber);
            requestParams.Add(Constants.UNSECURED_READY_PATH_URL_NONCE_LABEL, nonce);

            //Acquire dictionary keys and sort them.
            var paramsList = requestParams.Keys.ToList();
            paramsList.Sort();

            var sb = new StringBuilder(Crypt.Password);
            foreach (var key in paramsList)
            {
                sb.Append(requestParams[key]);
            }

            string hash = Crypt.Encrypt(sb.ToString());

            requestParams.Add(Constants.UNSECURED_READY_PATH_URL_HASH_LABEL, hash);
        }
    }
}
