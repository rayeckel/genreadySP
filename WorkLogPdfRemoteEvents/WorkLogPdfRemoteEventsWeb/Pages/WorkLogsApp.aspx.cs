using System;
using System.Web.UI;
using WorkLogPdfRemoteEventsWeb.ViewModels;

namespace WorkLogPdfRemoteEventsWeb
{
    public partial class WorkLogsApp : Page
    {
        private const string WORK_LOG_PARAM = "WorklogId";
        private const string IFRAME_ATTRIBUTE_SRC = "src";
        protected void Page_Load(object sender, EventArgs e)
        {
            var worklogId = (string)Request.QueryString[WORK_LOG_PARAM];

            var appViewModel = new WorkLogsAppViewModel(worklogId);

            string pdfUrl = appViewModel.GenerateSecurePdfUrl();

            pdfFrame.Attributes[IFRAME_ATTRIBUTE_SRC] = pdfUrl;
        }
    }
}