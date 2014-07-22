using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.SharePoint.Client;
using Microsoft.IdentityModel.S2S.Tokens;
using System.Net;
using System.IO;
using System.Xml;
using SystemLogs.LogWriter;

namespace SystemLogs.Pages
{
    public partial class Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            getAccessToken();
        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            Button1_ClickHandler();
        }

        private void getAccessToken()
        {
            string contextTokenString = TokenHelper.GetContextTokenFromRequest(Request);

            if (contextTokenString != null)
            {
                SharePointContextToken contextToken = TokenHelper.ReadAndValidateContextToken(contextTokenString, Request.Url.Authority);
                var sharepointUrl = new Uri(Request.QueryString["SPHostUrl"]);
                string accessToken = TokenHelper.GetAccessToken(contextToken, sharepointUrl.Authority).AccessToken;
                Button1.CommandArgument = accessToken;
            }
            else if (!IsPostBack)
            {
                Response.Write("Could not find a context token.");
                return;
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

            string accessToken = Button1.CommandArgument;

            var logWriter = new Log(accessToken);
            logWriter.WriteLog(title, Description);

            Label1.Text = Description;
        }
    }
}