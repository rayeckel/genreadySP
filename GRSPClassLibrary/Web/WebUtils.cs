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
        private const string UNSECURED_READY_PATH_URL_HASH_LABEL = "hash";
        private const string UNSECURED_READY_PATH_URL_NONCE_LABEL = "nonce";
        private const string CONTEXT_CREDENTIAL_USER_NAME = "readypath.account@generationready.com";
        private const string CONTEXT_CREDENTIAL_PASSWORD = "rsARgn5U";
        //private const string CONTEXT_CREDENTIAL_USER_NAME = "ray.eckel@generationreadydev.onmicrosoft.com";
        //private const string CONTEXT_CREDENTIAL_PASSWORD = "";

        private static SecureString CONTEXT_CREDENTIAL_PASSWORD_SECURE
        {
            get
            {
                var passWord = new SecureString();
                foreach (char c in CONTEXT_CREDENTIAL_PASSWORD.ToCharArray())
                {
                    passWord.AppendChar(c);
                }
                return passWord;
            }
        }

        public static void UploadFile(ClientContext clientContext, string listTitle, string sourceFileUrl, string libraryFileName, Dictionary<string, string> requestParams = null, Log.LogWriter syslogWriter = null)
        {
            string securedUrl = "";
            if (requestParams != null)
            {
                //Pass params Dictionary by reference since BuildPutRequestHash() adds hash and nonce
                BuildPutRequestHash(ref requestParams);

                securedUrl = GenerateSecureParamUrl(sourceFileUrl, requestParams);
            }

            var requestUri = new Uri(securedUrl.ToString());
            var request = CreateRequest(WebUtils.GET, requestUri);

            using(clientContext)
            using(var response = (HttpWebResponse)request.GetResponse())
            using(var receiveStream = (Stream)response.GetResponseStream())
            {
                //Establish permission to upload to the list.
                clientContext.Credentials =
                    new SharePointOnlineCredentials(WebUtils.CONTEXT_CREDENTIAL_USER_NAME, WebUtils.CONTEXT_CREDENTIAL_PASSWORD_SECURE);
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

        public static string GenerateSecureParamUrl(string sourceFileUrl, Dictionary<string, string> requestParams)
        {
            var securedUrl = new StringBuilder(sourceFileUrl);

            securedUrl.Append("?");
            foreach (KeyValuePair<string, string> urlParams in requestParams)
            {
                securedUrl.AppendFormat("&{0}={1}", urlParams.Key, urlParams.Value);
            }

            return securedUrl.ToString();
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
            var unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            string nonce = String.Format("{0}{1}", unixTimestamp, randomNumber);
            requestParams.Add(WebUtils.UNSECURED_READY_PATH_URL_NONCE_LABEL, nonce);

            //Acquire dictionary keys and sort them.
            var paramsList = requestParams.Keys.ToList();
            paramsList.Sort();

            var sb = new StringBuilder(Crypt.Password);
            foreach (var key in paramsList)
            {
                sb.Append(requestParams[key]);
            }

            string hash = Crypt.Encrypt(sb.ToString());
            requestParams[WebUtils.UNSECURED_READY_PATH_URL_HASH_LABEL] = hash;
        }
    }
}
