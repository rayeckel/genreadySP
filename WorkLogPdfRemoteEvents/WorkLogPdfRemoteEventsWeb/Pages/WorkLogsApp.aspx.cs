using System;
using WorkLogPdfRemoteEventsWeb.ViewModels;
using GRSPClassLibrary.Pages;

namespace WorkLogPdfRemoteEventsWeb
{
    public partial class WorkLogsApp : AccessTokenPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string SharepointUri = base.GetSharepointUri();
            var viewModel = new WorkLogsAppViewModel(SharepointUri, base.accessToken);

            viewModel.UploadPDF();
        }
    }
}