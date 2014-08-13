using System.Security;

namespace GRSPClassLibrary.Base
{
    public static class Constants
    {
        public const string CONTEXT_CREDENTIAL_USER_NAME = "readypath@generationready.onmicrosoft.com";
        public const string CONTEXT_CREDENTIAL_PASSWORD = "rsARgn5U";
        //public const string CONTEXT_CREDENTIAL_USER_NAME = "ray.eckel@generationreadydev.onmicrosoft.com";
        //public const string CONTEXT_CREDENTIAL_PASSWORD = "";
        public const string DBXL_DOC_SERVICE_PAGE = "DbxlDocumentService.asmx";
        public const string DBXL_ID_LABEL = "DbxlId";
        public const string DBXL_PROCESSING_INSTRUCTION_NAME = "QdabraDBXL";
        public const string DBXL_RECEIVER_NAME = "DbxlRER";
        public const string DBXL_SERVICE_URL_NAME = "DbxlRerServiceUrl";
        public const string DBXL_PASSWORD = "DBXLPassword";
        public const string DBXL_USERNAME = "DBXLUserName";
        public const string ERROR_LOG_LABEL = "Error Log";
        public const string KEY_DBXL_PROPERTY_DOCTYPE = "_DbxlDocType";
        public const string KEY_DBXL_PROPERTY_RER_ENABLED = "_DbxlRerEnabled";
        public const string SYSTEM_LOG_LABEL = "System Log";

        public static SecureString CONTEXT_CREDENTIAL_PASSWORD_SECURE
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
    }
}