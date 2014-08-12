using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint.Client;

namespace GRSPClassLibrary.Web
{
    public class DocumentUtils
    {
        public static byte[] GetDocumentByteSteam()
        {
            byte[] documentStream = new byte[];

            return documentStream;
        }
        public static void UploadFile(ClientContext clientContext, string listTitle, string filePath)
        {
            using (clientContext)
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open))
                {
                    var list = clientContext.Web.Lists.GetByTitle(listTitle);
                    clientContext.Load(list.RootFolder);
                    clientContext.ExecuteQuery();

                    Microsoft.SharePoint.Client.File.SaveBinaryDirect(clientContext, filePath, fileStream, true);
                }
            }
        }

        public static void UploadDocument(ClientContext clientContext, string siteURL, string documentListName, string documentListURL, string documentName, byte[] documentStream)
        {
            using (clientContext)
            {
                //Get Document List
                List documentsList = clientContext.Web.Lists.GetByTitle(documentListName);

                var fileCreationInformation = new FileCreationInformation();
                //Assign to content byte[] i.e. documentStream

                fileCreationInformation.Content = documentStream;
                //Allow owerwrite of document

                fileCreationInformation.Overwrite = true;
                //Upload URL

                fileCreationInformation.Url = siteURL + documentListURL + documentName;
                Microsoft.SharePoint.Client.File uploadFile = documentsList.RootFolder.Files.Add(
                    fileCreationInformation);

                //Update the metadata for a field having name "DocType"
                uploadFile.ListItemAllFields["DocType"] = "Favorites";

                uploadFile.ListItemAllFields.Update();
                clientContext.ExecuteQuery();

            }
        }
    }
}
