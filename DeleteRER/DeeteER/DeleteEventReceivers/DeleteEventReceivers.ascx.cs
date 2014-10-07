using System;
using System.ComponentModel;
using System.Web.UI.WebControls.WebParts;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint;
using System.Collections.Generic;
using System.Web.UI.WebControls;

namespace DeeteER.DeleteEventReceivers
{
    [ToolboxItemAttribute(false)]
    public partial class DeleteEventReceivers : WebPart
    {
        // Uncomment the following SecurityPermission attribute only when doing Performance Profiling on a farm solution
        // using the Instrumentation method, and then remove the SecurityPermission attribute when the code is ready
        // for production. Because the SecurityPermission attribute bypasses the security check for callers of
        // your constructor, it's not recommended for production purposes.
        // [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Assert, UnmanagedCode = true)]
        public DeleteEventReceivers()
        {
        }

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            InitializeControl();
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            using (SPSite site = SPContext.Current.Site)
            {
                SPWeb web = site.OpenWeb();


                SPListCollection lists = web.Lists;
                foreach (SPList list in lists)
                {
                    ddlList.Items.Add(list.Title);
                }
                web.Dispose();
            } 
        }

        protected void ddlList_SelectedIndexChanged(object sender, EventArgs e)
        {
            string listTitle = ddlList.SelectedValue;


            GetEventReceivers(listTitle);

        }

        protected void GetEventReceivers(string listTitle) 
        {
            using (SPSite site = SPContext.Current.Site)
            {
                SPWeb web = site.OpenWeb();
                SPList list = web.Lists[listTitle];
                var events = list.EventReceivers;
                ddlEventReceivers.Items.Clear();

                ListItem myListItem = new ListItem();

                foreach (SPEventReceiverDefinition oneEvent in events)
                {
                    myListItem.Text = oneEvent.Id.ToString();
                    myListItem.Value = oneEvent.Id.ToString();
                    ddlEventReceivers.Items.Add(myListItem);
                }
                web.Dispose();
            }
        }


        protected void btnDeleteER_Click(object sender, EventArgs e)
        {
            string listTitle = ddlList.SelectedValue;
            string receiverID = ddlEventReceivers.SelectedValue;

            using (SPSite site = SPContext.Current.Site)
            {
                SPWeb web = site.OpenWeb();
                SPList list = web.Lists[listTitle];

                var events = list.EventReceivers;
                if (DeleteAllER.Checked)
                {
                    foreach (SPEventReceiverDefinition oneEvent in events)
                    {
                            oneEvent.Delete();
                    }

                }
                else 
                {
                    foreach (SPEventReceiverDefinition oneEvent in events)
                    {
                        if (oneEvent.Id.ToString() == receiverID)
                        {
                            oneEvent.Delete();
                        }

                    }
                
                }


                ddlEventReceivers.Items.Clear();
                GetEventReceivers(listTitle);

                web.Dispose();
            }
        }
    }
}
