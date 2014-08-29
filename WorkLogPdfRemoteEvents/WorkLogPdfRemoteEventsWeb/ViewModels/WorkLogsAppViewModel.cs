using System;
using System.Collections.Generic;

namespace WorkLogPdfRemoteEventsWeb.ViewModels
{
    public class WorkLogsAppViewModel
    {
        #region Properties

        private string worklogId;
        private const string READY_PATH_SECURED_SOURCE_URL = "http://readypath.generationready.com/api/v1/worklogs";
        private const string READY_PATH_PDF_PATH = "/pdf";

        #endregion

        #region Constructors
        public WorkLogsAppViewModel()
        {}

        public WorkLogsAppViewModel(string Id)
        {
            this.worklogId = Id;
        }

        #endregion

        #region Methods

        public string GenerateSecurePdfUrl()
        {
            string sourceFileUrl = String.Format("{0}{1}/{2}", READY_PATH_SECURED_SOURCE_URL, READY_PATH_PDF_PATH, worklogId);
            var requestParams = new Dictionary<string, string>() { { "docName", worklogId }, { "param2", "pdf" } };

            //Pass params Dictionary by reference since BuildPutRequestHash() adds hash and nonce
            string securedUrl = GRSPClassLibrary.Web.WebUtils.GenerateSecureParamUrl(sourceFileUrl, requestParams);

            return securedUrl;
        }

        #endregion
    }
}