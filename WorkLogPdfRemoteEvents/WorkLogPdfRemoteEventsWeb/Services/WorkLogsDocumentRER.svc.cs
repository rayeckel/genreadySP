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
        private const string READY_PATH_SOURCE_URL = "readypath.generationready.com/api/v1/worklogs";
        private const string READY_PATH_SECURED_SOURCE_URL = "http://readypath.generationready.com/sauth/worklogs";
        private const string SITE_URL = "sites/re";
        private const string DOCUMENT_LIST_NAME = "Work Logs";
        private const string DOCUMENT_LIST_URL = "/Work Logs";
        private const string READY_PATH_PDF_PATH = "/pdf";
        private const string READY_PATH_EDIT_PATH = "/edit";
        private const string WORK_LOG_FILE_NAME = "WorkLogId";
        private const string WORK_LOG_STATUS_LABEL = "Status";
        private const string WORK_LOG_EDITABLE_LABEL = "Editable";
        private const string DOC_LIB_WORKLOG_ID_LABEL = "RPWorkLogId";

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
                            //syslogWriter.WriteLog("Work Logs Documents RER triggered", "Item Added");
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
            var documentName = (string)properties.ItemEventProperties.AfterProperties[WorkLogsDocumentRER.WORK_LOG_FILE_NAME];
            string sourceFileUrl = String.Format("{0}{1}/{2}", WorkLogsDocumentRER.READY_PATH_SECURED_SOURCE_URL, WorkLogsDocumentRER.READY_PATH_PDF_PATH, documentName);
            string libraryFileName = String.Format("/{0}/{1}/{2}.pdf", WorkLogsDocumentRER.SITE_URL, WorkLogsDocumentRER.DOCUMENT_LIST_NAME, documentName);
            var uploadUrlHashParams = new Dictionary<string, string>() { { "docName", documentName }, { "param2", "pdf" } };

            try
            {
                GRSPClassLibrary.Web.WebUtils.UploadFile(clientContext, WorkLogsDocumentRER.DOCUMENT_LIST_NAME, sourceFileUrl, libraryFileName, uploadUrlHashParams, syslogWriter);

                //Add some metadata
                Microsoft.SharePoint.Client.File newFile = clientContext.Web.GetFileByServerRelativeUrl(libraryFileName);
                clientContext.Load(newFile);
                clientContext.ExecuteQuery();

                newFile.ListItemAllFields[WorkLogsDocumentRER.DOC_LIB_WORKLOG_ID_LABEL] = documentName;
                newFile.ListItemAllFields.Update();
                clientContext.Load(newFile);
                clientContext.ExecuteQuery();
            }
            catch (Exception ex)
            {
                errorlogWriter.WriteLog("Work Logs Documents RER ERROR", ex.Message);
            }
        }

        private void UpdateItem(SPRemoteEventProperties properties, ClientContext clientContext)
        {
            var workLogId = (string)properties.ItemEventProperties.AfterProperties[WorkLogsDocumentRER.WORK_LOG_FILE_NAME];
            string readyPathUpdateUrl = String.Format("{0}{1}/{2}", WorkLogsDocumentRER.READY_PATH_SECURED_SOURCE_URL, WorkLogsDocumentRER.READY_PATH_EDIT_PATH, workLogId);

            ListItem itemUpdating = GetFormsListItem(properties, clientContext);

            if (itemUpdating != null)
            {
                var newWorkLogStatus = (string)properties.ItemEventProperties.AfterProperties[WorkLogsDocumentRER.WORK_LOG_STATUS_LABEL];
                var oldWorkLogStatus = (string)itemUpdating[WorkLogsDocumentRER.WORK_LOG_STATUS_LABEL];
                if(oldWorkLogStatus == null)
                {
                    oldWorkLogStatus = "";
                }
                var updatingStatus = newWorkLogStatus != oldWorkLogStatus;

                var newWorkLogEditable = (string)properties.ItemEventProperties.AfterProperties[WorkLogsDocumentRER.WORK_LOG_EDITABLE_LABEL];
                var oldWorkLogEditable = (string)itemUpdating[WorkLogsDocumentRER.WORK_LOG_EDITABLE_LABEL];
                if (oldWorkLogEditable == null)
                {
                    oldWorkLogEditable = "";
                }
                var updatingEditable = newWorkLogEditable != oldWorkLogEditable;

                if (updatingStatus || updatingEditable)
                {
                    var updateParams =
                        new Dictionary<string, string>() { { WorkLogsDocumentRER.WORK_LOG_STATUS_LABEL, newWorkLogStatus }, { WorkLogsDocumentRER.WORK_LOG_EDITABLE_LABEL, newWorkLogEditable } };
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
