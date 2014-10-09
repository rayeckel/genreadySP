<#
.SYNOPSIS
   <A brief description of the script>
.DESCRIPTION
   <A detailed description of the script>
.PARAMETER <paramName>
   <Description of script parameter>
.EXAMPLE
   <An example of using the script>
#>
# load in sharepoint classes 
# the path here may need to change if you used e.g. C:\Lib.. 
# note that you might need some other references (depending on what your script does):
Add-Type -Path "c:\Program Files\Common Files\Microsoft Shared\Web Server Extensions\15\ISAPI\Microsoft.SharePoint.Client.dll" 
Add-Type -Path "c:\Program Files\Common Files\Microsoft Shared\Web Server Extensions\15\ISAPI\Microsoft.SharePoint.Client.Runtime.dll" 
Add-Type -Path "c:\Program Files\Common Files\Microsoft Shared\Web Server Extensions\15\ISAPI\Microsoft.SharePoint.Client.Taxonomy.dll" 

# SP context info (consider using Get-Credential to enter password securely as script runs).. 
$SPurl = "https://generationready.sharepoint.com/reporting"
$SPusername = "ray.eckel@generationready.com"
$SPpassword = "Beerme2day!" 
$SPRootUrl = "https://generationready.sharepoint.com"
$SPSiteUrl = "/reporting"

function Main()
{
	# get web
	$clientContext = GetSharePointContext
	$web = $clientContext.Web
	$properties = $web.AllProperties

	$clientContext.Load($web)
	$clientContext.Load($properties)
	
	$clientContext.ExecuteQuery()
	
	Write-Host "ReportServerUrl = " $properties.FieldValues["ReportServerUrl"] -ForegroundColor Green
	Write-Host "RS_Username = " $properties.FieldValues["RS_Username"] -ForegroundColor Green
	Write-Host "RS_Password = " $properties.FieldValues["RS_Password"] -ForegroundColor Green
}

function GetSharePointContext
{
	$securePassword = ConvertTo-SecureString $SPpassword -AsPlainText -Force 
	
	# connect/authenticate to SharePoint Online and get ClientContext object.. 
	$clientContext = New-Object Microsoft.SharePoint.Client.ClientContext($SPurl) 
	$credentials = New-Object Microsoft.SharePoint.Client.SharePointOnlineCredentials($SPusername, $securePassword)
	$clientContext.Credentials = $credentials

	return $clientContext
}

Main