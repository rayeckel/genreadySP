using System;
using Microsoft.SharePoint.Client;
using WorkLogPdfRemoteEventsWeb.Base;
using GRSPClassLibrary.Web;

namespace WorkLogPdfRemoteEventsWeb.ViewModels
{
    public class WorkLogsAppViewModel : ClientAccess
    {
        public WorkLogsAppViewModel(string accessToken, string SharepointUri)
            : base(accessToken, SharepointUri)
        { }

        public void UploadPDF()
        {
            ClientContext clientContext = base.GetClientAccessContextWithToken();
            string documentName = "6";
            var fileUrl = String.Format("{0}/{1}", Constants.SOURCE_URL, documentName);

            GRSPClassLibrary.Web.DocumentUtils.UploadFile(clientContext, Constants.DOCUMENT_LIST_NAME, fileUrl);
        }
    }
}