using System;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.EventReceivers;
using System.Xml;
using DBXLClassLibrary.DbxlDocumentService;
using FormLibraryEventReceiverWeb.Base;

namespace FormLibraryEventReceiverWeb.Services
{
    public class DbxlRER : GRSPClassLibrary.Dbxl.EventReceiver
    {
        protected override void ExecuteRER(SPRemoteEventProperties properties, ClientContext clientContext)
        {
            if (!RERIsEnabled(properties, clientContext))
            {
                return;
            }

            clientContext.Load(clientContext.Web, web => web.Lists);
            clientContext.ExecuteQuery();

            //get Dbxl document type for list
            string DbxlDocTypeProperty = properties.ItemEventProperties.ListId + GRSPClassLibrary.Base.Constants.KEY_DBXL_PROPERTY_DOCTYPE;
            string DbxlDocType = GRSPClassLibrary.Dbxl.Properties.GetDbxlProperty(DbxlDocTypeProperty, clientContext);

            Guid listId = properties.ItemEventProperties.ListId;
            int Id = properties.ItemEventProperties.ListItemId;
            ListItem listItem = ClientContextListItem(clientContext, listId, Id);
            XmlDocument Doc = LoadClientFile(clientContext, listItem);
            IDbxlDocumentService DocService = CredentialDocumentService(clientContext);

            var itemEditorLookupValue = (FieldUserValue)listItem[Constants.LIST_ITEM_EDITOR];
            string itemEditor = itemEditorLookupValue.LookupValue;

            switch (properties.EventType)
            {
                case SPRemoteEventType.ItemAdded:
                    {
                        try
                        {
                            int DbxlId;
                            string RefId;
                            string DbxlDescriptionText = "Item Added: " + DateTime.Now;
                            StatusInfo SubmitResult =
                                DocService.SubmitDocument(DbxlDocType, Doc.OuterXml, Id.ToString(), itemEditor, DbxlDescriptionText, Constants.TRUE, out DbxlId, out RefId);

                            if (SubmitResult.Success)
                            {
                                syslogWriter.WriteLog("DBXL RER Triggered", "Item added");

                                listItem[GRSPClassLibrary.Base.Constants.DBXL_ID_LABEL] = DbxlId.ToString();
                                listItem.Update();
                                clientContext.ExecuteQuery();
                            }
                            else if (!SubmitResult.Success)
                            {
                                errorlogWriter.WriteLog("DBXL RER Item Added ERROR", SubmitResult.Errors[0].Description);
                            }
                        }
                        catch (Exception ex)
                        {
                            errorlogWriter.WriteLog("DBXL RER Item Added ERROR", ex.Message);
                        }

                        break;
                    }
                case SPRemoteEventType.ItemUpdated:
                    {
                        try
                        {
                            //Add qdabra processing instruction
                            int DbxlId = Convert.ToInt32(listItem[GRSPClassLibrary.Base.Constants.DBXL_ID_LABEL].ToString());
                            DbxlPiXmlProcessingInstruction(Doc, DbxlId, DbxlDocType);

                            string RefId;
                            string DbxlDescriptionText = "Item Updated: " + DateTime.Now;
                            StatusInfo SubmitResult =
                                DocService.SubmitDocument(DbxlDocType, Doc.OuterXml, Id.ToString(), itemEditor, DbxlDescriptionText, Constants.TRUE, out DbxlId, out RefId);

                            if (SubmitResult.Success)
                            {
                                syslogWriter.WriteLog("DBXL RER Triggered", "Item updated");

                                //This is triggering an endless loop.
                                //listItem[GRSPClassLibrary.Base.Constants.DBXL_ID_LABEL] = DbxlId.ToString();                                
                                //listItem[Constants.LIST_ITEM_EDITED_BY] = itemEditor;
                                //listItem.Update();
                                //clientContext.ExecuteQuery();
                            }
                            else if (!SubmitResult.Success)
                            {
                                errorlogWriter.WriteLog("DBXL RER Item Updated ERROR", SubmitResult.Errors[0].Description);
                            }
                        }
                        catch (Exception ex)
                        {
                            errorlogWriter.WriteLog("DBXL RER Item Updated ERROR", ex.Message);
                        }

                        break;
                    }
                case SPRemoteEventType.ItemDeleting:
                    {
                        try
                        {
                            //Add qdabra processing instruction
                            int DbxlId = Convert.ToInt32(listItem[GRSPClassLibrary.Base.Constants.DBXL_ID_LABEL].ToString());
                            StatusInfo SubmitResult = DocService.RemoveDocument(DbxlId);

                            if (SubmitResult.Success)
                            {
                                syslogWriter.WriteLog("DBXL RER Triggered", "Item deleted");
                            }
                            else if (!SubmitResult.Success)
                            {
                                errorlogWriter.WriteLog("DBXL RER Item Deleting ERROR", SubmitResult.Errors[0].Description);
                            }
                        }
                        catch (Exception ex)
                        {
                            errorlogWriter.WriteLog("DBXL RER Item Deleting ERROR", ex.Message);
                        }

                        break;
                    }
                default:
                    {
                        errorlogWriter.WriteLog("DBXL Remote Event Receiver ERROR", "No Remote Event Type Provided.");
                        break;
                    }
            }
        }
    }
}
