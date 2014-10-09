<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ReportsApp.aspx.cs" Inherits="ReportsAppPartWeb.Pages.Home.ReportsApp" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Report Server Config App</title>
    <script type="text/javascript" src="//ajax.aspnetcdn.com/ajax/4.0/1/MicrosoftAjax.js"></script>
    <script type="text/javascript" src="/Scripts/jquery-1.8.2.min.js"></script>
    <script type="text/javascript" src="/Scripts/json2.js"></script>
    <script type="text/javascript" src="/Scripts/chromeLoader.js"></script>
    <script type="text/javascript" src="/Scripts/jquery.signalR-1.0.0.js"></script>
    <script type="text/javascript" src="/signalr/hubs"></script>
</head>
<body>
    <form id="form2" runat="server">
    <h1>Report Server Config App</h1>
    <div="MainContent2">        
        <p>
            <asp:HyperLink ID="LnkHome" runat="server">Home</asp:HyperLink>
        </p>
        <p>            
            Report Server URL: <asp:TextBox ID="rptServerUrl" runat="server"></asp:TextBox>
            <br />
            <br />
            Report Server User Name: <asp:TextBox ID="TxtUsername" runat="server"></asp:TextBox>
            <br />
            Report Server Password: <asp:TextBox ID="TxtPassword" runat="server"></asp:TextBox>
        </p>
        <p>
            <asp:Button ID="btn_SaveSettings" runat="server" Text="Save Settings" OnClick="btn_SaveSettings_Click" />
        </p>

        <asp:RequiredFieldValidator id="RequiredFieldValidator1" runat="server"
            ControlToValidate="rptServerUrl"
            ErrorMessage="Report Server URL is a required field."
            ForeColor="Red">
        </asp:RequiredFieldValidator>
        <asp:RequiredFieldValidator id="RequiredFieldValidator2" runat="server"
            ControlToValidate="TxtUsername"
            ErrorMessage="Report Server User Name is a required field."
            ForeColor="Red">
        </asp:RequiredFieldValidator>
        <asp:RequiredFieldValidator id="RequiredFieldValidator3" runat="server"
            ControlToValidate="TxtPassword"
            ErrorMessage="Report Server Password is a required field."
            ForeColor="Red">
        </asp:RequiredFieldValidator>
    </div>
    </form>
</body>
</html>
