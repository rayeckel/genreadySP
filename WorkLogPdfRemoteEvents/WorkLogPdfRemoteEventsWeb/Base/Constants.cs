using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WorkLogPdfRemoteEventsWeb.Base
{
    public static class Constants
    {
        //public const string READY_PATH_SOURCE_URL = "http://readypath.generationready.com/api/v1/worklogs";
        public const string READY_PATH_UNSECURED_SOURCE_URL = "http://readypath.generationready.com/sauth/worklogs";
        public const string READY_PATH_SOURCE_URL = "http://localstash:8888/worklogs";
        public const string SITE_URL = "sites/re";
        public const string DOCUMENT_LIST_NAME = "Work Logs";
        public const string DOCUMENT_LIST_URL = "/Work Logs";
        public const string READY_PATH_PDF_PATH = "/pdf";
        public const string READY_PATH_EDIT_PATH = "/edit";
        public const string WORK_LOG_FILE_NAME = "WorkLogId";
        public const string WORK_LOG_STATUS_LABEL = "Status";
        public const string WORK_LOG_EDITABLE_LABEL = "Editable";
        public const string WORKLOGS_LIBRARY_NAME = "ReadyPathWorkLogs";
        public const string WORKLOGS_RECEIVER_NAME = "WorkLogsDocumentRER";
        public const string DOC_LIB_WORKLOG_ID_LABEL = "RPWorkLogId";
    }
}