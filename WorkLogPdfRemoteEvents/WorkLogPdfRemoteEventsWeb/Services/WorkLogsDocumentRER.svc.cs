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
                            UpdateItem(properties, clientContext);
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
            string sourceFileUrl = String.Format("{0}{1}/{2}.pdf", Constants.READY_PATH_UNSECURED_SOURCE_URL, Constants.READY_PATH_PDF_PATH, documentName);
            string libraryFileName = String.Format("/{0}/{1}/{2}.pdf", Constants.SITE_URL, Constants.DOCUMENT_LIST_NAME, documentName);
            var uploadUrlHashParams = new Dictionary<string, string>() { { "docName", documentName }, { "param2", "pdf" } };

            try
            {
                GRSPClassLibrary.Web.WebUtils.UploadFile(clientContext, Constants.DOCUMENT_LIST_NAME, sourceFileUrl, libraryFileName, uploadUrlHashParams);
            }
            catch (Exception ex)
            {
                errorlogWriter.WriteLog("Work Logs Documents RER ERROR", ex.Message);
            }
        }

        private void UpdateItem(SPRemoteEventProperties properties, ClientContext clientContext)
        {
            var workLogId = (string)properties.ItemEventProperties.AfterProperties[Constants.WORK_LOG_FILE_NAME];
            string readyPathUpdateUrl = String.Format("{0}{1}/{2}", Constants.READY_PATH_UNSECURED_SOURCE_URL, Constants.READY_PATH_EDIT_PATH, workLogId);

            ListItem itemUpdating = GetFormsListItem(properties, clientContext);

            if (itemUpdating != null)
            {
                var newWorkLogStatus = (string)properties.ItemEventProperties.AfterProperties[Constants.WORK_LOG_STATUS_LABEL];
                var oldWorkLogStatus = (string)itemUpdating[Constants.WORK_LOG_STATUS_LABEL];
                var updatingStatus = newWorkLogStatus != oldWorkLogStatus;

                var newWorkLogEditable = (string)properties.ItemEventProperties.AfterProperties[Constants.WORK_LOG_EDITABLE_LABEL];
                var oldWorkLogEditable = (string)itemUpdating[Constants.WORK_LOG_EDITABLE_LABEL];
                var updatingEditable = newWorkLogEditable != oldWorkLogEditable;

                if (updatingStatus || updatingEditable)
                {
                    var updateParams = 
                        new Dictionary<string, string>() { { Constants.WORK_LOG_STATUS_LABEL, newWorkLogStatus }, { Constants.WORK_LOG_EDITABLE_LABEL, newWorkLogEditable } };
                    try
                    {
                        GRSPClassLibrary.Web.WebUtils.PutData(readyPathUpdateUrl, updateParams);
                    }
                    catch (Exception ex)
                    {
                        errorlogWriter.WriteLog("Work Logs Documents RER ERROR", ex.Message);
                    }
                }
                else
                {
                    UploadPDF(properties, clientContext);
                }
            }
        }

        private ListItem GetFormsListItem(SPRemoteEventProperties properties, ClientContext clientContext)
        {
            ListCollection webLists = clientContext.Web.Lists;
            List formsList = webLists.GetByTitle(Constants.WORKLOGS_LIBRARY_NAME);
            string itemId = Convert.ToString(properties.ItemEventProperties.ListItemId);

            var formsQuery = new CamlQuery();
            formsQuery.ViewXml = "<View><Query><Where><Eq><FieldRef Name='ID'/>" +
                "<Value Type='Number'>" + itemId + "</Value></Eq></Where></Query></View>";
            ListItemCollection query = formsList.GetItems(formsQuery);

            clientContext.Load(query);
            clientContext.ExecuteQuery();

            if (query.Count() > 0)
            {
                return query.First();
            }

            return null;
        }
    }
}
