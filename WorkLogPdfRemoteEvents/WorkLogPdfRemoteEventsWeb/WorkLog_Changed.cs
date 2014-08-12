        /// <summary>
        /// Validates file extension as pdf
        /// Saves to separate library, to reduce file size for infopath form
        /// Removes attachment
        /// Links to work log in work log library
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void WorkLog_Changed(object sender, XmlEventArgs e)
        {
            XPathNavigator Navigator = MainDataSource.CreateNavigator();
            XPathNavigator AttachmentNode = Navigator.SelectSingleNode("/my:Root/my:WorkLog", NamespaceManager);
            if (AttachmentNode != null && !String.IsNullOrEmpty(AttachmentNode.Value))
            {
                // get attachment
                InfoPathAttachmentEncoding.InfoPathAttachmentDecoder Decoded = new InfoPathAttachmentEncoding.InfoPathAttachmentDecoder(AttachmentNode.Value);
                // check if pdf
                if (Path.GetExtension(Decoded.Filename).Equals(".pdf"))
                {
                    // set alert message to blank
                    Navigator.SelectSingleNode("/my:Root/my:Message", NamespaceManager).SetValue(String.Empty);
                    //  decode attachment and add to byte array, get filename
                    byte[] Data = Decoded.DecodedAttachment;
                    string Guid = Navigator.SelectSingleNode("/my:Root/my:Guid", NamespaceManager).Value;
                    using (SPSite Site = new SPSite("https://generationready.sharepoint.com/workmanagement"))
                    {
                        using (SPWeb Web = Site.OpenWeb())
                        {
                            // specify folder for upload
                            SPFolder WorkLogLibrary = Web.Folders["Work Logs"];
                            try
                            {
                                // get logged in user information for metadata
                                String Username = Navigator.SelectSingleNode("/my:Root/my:Username", NamespaceManager).Value;
                                SPUser User = Web.EnsureUser(Username);
                                SPFieldUserValue UserValue = new SPFieldUserValue(Web, User.ID, User.LoginName);
                                Web.AllowUnsafeUpdates = true;
                                // set filename and add file to library, overwrite if already exists
                                SPFile File = WorkLogLibrary.Files.Add(Guid + ".pdf", Data, true);
                                
                                // get list item and add metadata
                                SPListItem Item = File.GetListItem();
                                //Item["Date"] = Convert.ToDateTime(Navigator.SelectSingleNode("/my:Root/my:EngagementDate", NamespaceManager).Value);
                                Item["Allocation Id"] = Navigator.SelectSingleNode("/my:Root/my:AllocationId", NamespaceManager).Value;
                                Item["Consultant"] = UserValue;
                                Navigator.SelectSingleNode("/my:Root/my:WorkLogItemId", NamespaceManager).SetValue(Item.ID.ToString());
                                Item.Update();
                                Web.AllowUnsafeUpdates = false;
                                // remove attachment from form
                                AttachmentNode.SetValue(String.Empty);                                
                            }
                            catch (Exception ex)
                            {
                                Navigator.SelectSingleNode("/my:Root/my:Status", NamespaceManager).SetValue(ex.Message.ToString() + ";" + ex.InnerException.Message);
                                //set_str_value("/my:Root/my:Status", ex.Message.ToString() + ";" + ex.InnerException.Message);
                            }
                        }
                    }
                }
                // if not pdf alert user
                else
                {
                    Navigator.SelectSingleNode("/my:Root/my:Message", NamespaceManager).SetValue("The work log must be in PDF format.  Please change the format and attach it again.");
                    AttachmentNode.SetValue(String.Empty);
                }
            }
        }