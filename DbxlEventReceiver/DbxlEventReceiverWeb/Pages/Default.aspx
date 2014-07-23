<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="DbxlEventReceiverWeb.Default" %>

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
            Enable DBXL Remote Event Receivers: <asp:CheckBox ID="CbxRerEnabled" runat="server" />
            <br />
            DBXL Document Type: <asp:TextBox ID="TxtDocType" runat="server"></asp:TextBox>
        </p>
        <p>
            <asp:Button ID="SaveSettings" runat="server" Text="Save Settings" OnClick="SaveDbxlSettings" />
        </p>
    </div>
    </form>
</body>
</html>
