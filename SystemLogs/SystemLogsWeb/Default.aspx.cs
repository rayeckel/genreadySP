using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using Microsoft.SharePoint.Client;
using System.Net;
using System.IO;
using System.Xml;
using SystemLogs.Log;
using GRSPClassLibrary.Pages;

namespace SystemLogs.Pages
{
    public partial class Default : AccessTokenPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            setAccessToken();
        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            Button1_ClickHandler();
        }

        private void setAccessToken()
        {
            if (base.accessToken != "")
            {
                this.Button1.CommandArgument = base.accessToken;
            }
        }

        private void Button1_ClickHandler()
        {
            string fName = TextBox1.Text;
            string lname = TextBox2.Text;
            string favColor = DropDownList1.SelectedValue;
            string luckyNum = DropDownList2.SelectedValue;

            string title = "User input Log";
            string Description = fName + " " + lname + " Favorite Color: " + favColor + "  Lucky Number: " + luckyNum;

            this.writeToLog(title, Description);

            Label1.Text = Description;
        }

        private void writeToLog(string Title, string Description)
        {
            string accessToken = this.Button1.CommandArgument;
            var logWriter = new LogWriter(accessToken);
            logWriter.WriteLog(Title, Description);
        }
    }
}