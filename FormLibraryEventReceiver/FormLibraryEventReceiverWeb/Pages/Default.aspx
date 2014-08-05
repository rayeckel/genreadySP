<%@ Page Trace="false" Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="FormLibraryEventReceiverWeb.Pages.Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Dbxl Event Receiver App</title>
    <script type="text/javascript" src="//ajax.aspnetcdn.com/ajax/4.0/1/MicrosoftAjax.js"></script>
    <script type="text/javascript" src="/Scripts/jquery-1.8.2.min.js"></script>
    <script type="text/javascript" src="/Scripts/json2.js"></script>
    <script type="text/javascript" src="/Scripts/chromeLoader.js"></script>
    <script type="text/javascript" src="/Scripts/jquery.signalR-1.0.0.js"></script>
    <script type="text/javascript" src="/signalr/hubs"></script>
</head>
<body>
    <form id="form1" runat="server">
    <h1>Dbxl Event Recevier App</h1>
    <div="MainContent">        
        <p>
            <asp:HyperLink ID="LnkHome" runat="server">Home</asp:HyperLink>
        </p>
        <p>
            Library Id: <asp:Label ID="LblListGuid" runat="server"></asp:Label>
            <br />
            Library Title: <asp:Label ID="LblListTitle" runat="server"></asp:Label>
        </p>
        <p>            
            Enable DBXL Remote Event Receivers: 
            <asp:CheckBox ID="CbxRerEnabled" runat="server" />
            <br />
            DBXL Service URL: 
            <asp:TextBox ID="TxtServiceUrl" runat="server"></asp:TextBox>
            <br />
            <br />
            DBXL Document Type: 
            <asp:TextBox ID="TxtDocType" runat="server"></asp:TextBox>
            <br />
            <br />
            DBXL User Name: <asp:TextBox ID="TxtUsername" runat="server"></asp:TextBox>
            <br />
            DBXL Password: <asp:TextBox ID="TxtPassword" runat="server"></asp:TextBox>
        </p>
        <p>
            <asp:Button ID="btn_SaveSettings" runat="server" Text="Save Settings" OnClick="btn_SaveSettings_Click" />
        </p>

        <asp:RequiredFieldValidator id="RequiredFieldValidator1" runat="server"
            ControlToValidate="TxtDocType"
            ErrorMessage="DBXL Document Type is a required field."
            ForeColor="Red">
        </asp:RequiredFieldValidator>
        <asp:RequiredFieldValidator id="RequiredFieldValidator2" runat="server"
            ControlToValidate="TxtUsername"
            ErrorMessage="DBXL User Name is a required field."
            ForeColor="Red">
        </asp:RequiredFieldValidator>
        <asp:RequiredFieldValidator id="RequiredFieldValidator3" runat="server"
            ControlToValidate="TxtPassword"
            ErrorMessage="DBXL Password is a required field."
            ForeColor="Red">
        </asp:RequiredFieldValidator>
    </div>
    </form>
</body>
</html>
