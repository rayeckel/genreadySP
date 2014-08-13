﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WorkLogPdfRemoteEventsWeb.Base
{
    public static class Constants
    {
        public const string SOURCE_URL = "http://readypath.generationready.com/api/v1/worklogs/pdf";
        //public const string SOURCE_URL = "http://localstash:8888/worklogs/pdf";
        public const string SITE_URL = "sites/re";
        public const string DOCUMENT_LIST_NAME = "Work Logs";
        public const string DOCUMENT_LIST_URL = "/Work Logs";
        public const string WORK_LOG_FILE_NAME = "WorkLogId";
        public const string WORKLOGS_LIBRARY_NAME = "ReadyPathWorkLogs";
        public const string WORKLOGS_RECEIVER_NAME = "WorkLogsDocumentRER";
    }
}