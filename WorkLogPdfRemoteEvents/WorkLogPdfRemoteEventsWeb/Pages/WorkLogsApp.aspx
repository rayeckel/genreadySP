<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="WorkLogsApp.aspx.cs" Inherits="WorkLogPdfRemoteEventsWeb.WorkLogsApp" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head>
    <title>Work Logs Viewer</title>
</head>
    <body>
        <div>
            <center><iframe ID="pdfFrame" src='' runat="server" style="width:1000px; height:1000px;" frameborder="0"></iframe></center>
        </div>
    </body>
</html>
