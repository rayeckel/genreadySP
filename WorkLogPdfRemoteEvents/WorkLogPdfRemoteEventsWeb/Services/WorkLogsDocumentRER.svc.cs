using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.EventReceivers;
using GRSPClassLibrary.Web;
using WorkLogPdfRemoteEventsWeb.Base;

namespace WorkLogPdfRemoteEventsWeb.Services
{
    public class WorkLogsDocumentRER : GRSPEventReciever
    {
        protected override void ExecuteRER(SPRemoteEventProperties properties, ClientContext clientContext)
        {
            clientContext.Load(clientContext.Web, web => web.Lists);
            clientContext.ExecuteQuery();

            switch (properties.EventType)
            {
                case SPRemoteEventType.ItemAdding:
                    {
                        try
                        {
                            UploadPDF(properties, clientContext);
                            syslogWriter.WriteLog("Work Logs Documents RER triggered", "Item Added");
                        }
                        catch (Exception ex)
                        {
                            errorlogWriter.WriteLog("Work Logs Documents RER Item Added ERROR", ex.Message);
                        }

                        break;
                    }
                case SPRemoteEventType.ItemUpdating:
                    {
                        try
                        {
                            UploadPDF(properties, clientContext);
                            syslogWriter.WriteLog("Work Logs Documents RER  triggered", "Item Updated");
                        }
                        catch (Exception ex)
                        {
                            errorlogWriter.WriteLog("Work Logs Documents RER Item Updated ERROR", ex.Message);
                        }

                        break;
                    }
                case SPRemoteEventType.ItemDeleting:
                    {
                        try
                        {
                            syslogWriter.WriteLog("Work Logs Documents RER  triggered", "Item Deleting");
                        }
                        catch (Exception ex)
                        {
                            errorlogWriter.WriteLog("Work Logs Documents RER Item Deleting ERROR", ex.Message);
                        }

                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }
        private void UploadPDF(SPRemoteEventProperties properties, ClientContext clientContext)
        {
            var documentName = (string)properties.ItemEventProperties.AfterProperties[Constants.WORK_LOG_FILE_NAME];
            string sourceFileUrl = String.Format("{0}/{1}.pdf", Constants.SOURCE_URL, documentName);
            string libraryFileName = String.Format("/{0}/{1}/{2}.pdf", Constants.SITE_URL, Constants.DOCUMENT_LIST_NAME, documentName);

            try
            {
                GRSPClassLibrary.Web.DocumentUtils.UploadFile(clientContext, Constants.DOCUMENT_LIST_NAME, sourceFileUrl, libraryFileName);
            }
            catch (Exception ex)
            {
                errorlogWriter.WriteLog("Work Logs Documents RER ERROR", ex.Message);
            }
        }
    }
}
