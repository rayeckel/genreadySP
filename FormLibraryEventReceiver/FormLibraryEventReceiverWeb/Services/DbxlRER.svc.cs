using System;
using System.Linq;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.EventReceivers;
using System.Xml;
using DBXLClassLibrary.DbxlDocumentService;
using FormLibraryEventReceiverWeb.Base;
using System.Collections.Generic;

namespace FormLibraryEventReceiverWeb.Services
{
    public class DbxlRER : GRSPClassLibrary.Dbxl.EventReceiver
    {
        private const string ALLOCATION_ID_LABEL = "AllocationId";
        private const string UNIT_LABEL = "Unit";

        protected override void ExecuteRER(SPRemoteEventProperties properties, ClientContext clientContext)
        {
            if (properties.EventType == SPRemoteEventType.ItemAdding || !RERIsEnabled(properties, clientContext))
            {
                return;
            }

            clientContext.Load(clientContext.Web, web => web.Lists);
            clientContext.ExecuteQuery();

            string itemEditor = (string)properties.ItemEventProperties.UserDisplayName;

            if (properties.EventType == SPRemoteEventType.ItemUpdating)
            {
                result.ChangedItemProperties.Add(Constants.LIST_ITEM_EDITED_BY, itemEditor);
            }
            else
            {
                //get Dbxl document type for list
                string DbxlDocTypeProperty = properties.ItemEventProperties.ListId + GRSPClassLibrary.Base.Constants.KEY_DBXL_PROPERTY_DOCTYPE;
                string DbxlDocType = GRSPClassLibrary.Dbxl.Properties.GetDbxlProperty(DbxlDocTypeProperty, clientContext);

                Guid listId = properties.ItemEventProperties.ListId;
                int Id = properties.ItemEventProperties.ListItemId;
                IDbxlDocumentService DocService = CredentialDocumentService(clientContext);
                ListItem listItem = ClientContextListItem(clientContext, listId, Id);
                XmlDocument Doc = LoadClientFile(clientContext, listItem);

                ListCollection webLists = clientContext.Web.Lists;
                List activeFormLibrary = webLists.GetById(listId);
                clientContext.Load(activeFormLibrary);
                EventReceiverDefinitionCollection erdCollection = activeFormLibrary.EventReceivers;
                clientContext.Load(erdCollection);
                clientContext.ExecuteQuery();

                switch (properties.EventType)
                {
                    //MUST use ItemAdded BC we need the list item ID for DBXL 
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
                                    listItem[Constants.LIST_ITEM_EDITED_BY] = itemEditor;
                                    listItem.Update();

                                    //Borrow an unused event type to act as a flag to indicate "EventFiringEnabled = false"
                                    erdCollection.Add(new EventReceiverDefinitionCreationInformation() { EventType = EventReceiverType.InvalidReceiver });
                                    
                                    clientContext.ExecuteQuery();
                                }
                                else if (!SubmitResult.Success)
                                {
                                    errorlogWriter.WriteLog("DBXL RER Item Added - DBXL Submit ERROR", SubmitResult.Errors[0].Description);
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
                                //Update is triggered when this RER adds the DbxlId (above). If EventReceiverType.InvalidReceiver is registered, interpret this as a
                                //signal to ignore update.
                                EventReceiverDefinition eventFiringDisabled = erdCollection.FirstOrDefault(er => (er.EventType == EventReceiverType.InvalidReceiver));

                                if (eventFiringDisabled == null)
                                {
                                    //Add qdabra processing instruction
                                    int DbxlId = Convert.ToInt32(listItem[GRSPClassLibrary.Base.Constants.DBXL_ID_LABEL]);
                                    DbxlPiXmlProcessingInstruction(Doc, DbxlId, DbxlDocType);

                                    string RefId;
                                    string DbxlDescriptionText = "Item Updating: " + DateTime.Now;
                                    StatusInfo SubmitResult =
                                        DocService.SubmitDocument(DbxlDocType, Doc.OuterXml, Id.ToString(), itemEditor, DbxlDescriptionText, Constants.TRUE, out DbxlId, out RefId);

                                    if (SubmitResult.Success)
                                    {
                                        syslogWriter.WriteLog("DBXL RER Triggered", "Item updated");
                                    }
                                    else if (!SubmitResult.Success)
                                    {
                                        errorlogWriter.WriteLog("DBXL RER Item Updating - DBXL Submit ERROR", SubmitResult.Errors[0].Description);
                                    }
                                }
                                else
                                {
                                    eventFiringDisabled.DeleteObject();
                                }
                            }
                            catch (Exception ex)
                            {
                                errorlogWriter.WriteLog("DBXL RER Item Updating ERROR", ex.Message);
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
                            break;
                        }
                }
            }
        }

        private void processItemUpdated()
        { 
        }
    }
}
