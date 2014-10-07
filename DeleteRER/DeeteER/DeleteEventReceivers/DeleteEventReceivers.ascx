<%@ Assembly Name="$SharePoint.Project.AssemblyFullName$" %>
<%@ Assembly Name="Microsoft.Web.CommandUI, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> 
<%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> 
<%@ Register Tagprefix="Utilities" Namespace="Microsoft.SharePoint.Utilities" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="asp" Namespace="System.Web.UI" Assembly="System.Web.Extensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" %>
<%@ Import Namespace="Microsoft.SharePoint" %> 
<%@ Register Tagprefix="WebPartPages" Namespace="Microsoft.SharePoint.WebPartPages" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="DeleteEventReceivers.ascx.cs" Inherits="DeeteER.DeleteEventReceivers.DeleteEventReceivers" %>
Get all Lists
<br />
<asp:DropDownList ID="ddlList" runat="server" OnSelectedIndexChanged="ddlList_SelectedIndexChanged" AutoPostBack="True"></asp:DropDownList>
<p>
    &nbsp;</p>
<p>
    Get all Event Receivers
</p>
<p>
    <asp:DropDownList ID="ddlEventReceivers" runat="server">
    </asp:DropDownList>
</p>
<p>
    <asp:CheckBox ID="DeleteAllER" runat="server" Text="Delete All Event Receivers" Visible="false" />
</p>
<asp:Button ID="btnDeleteER" runat="server" Text="Delete Event Receiver" OnClick="btnDeleteER_Click" />
