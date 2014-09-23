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
$SPurl = "https://generationreadydev.sharepoint.com"
$SPusername = "ray.eckel@generationreadydev.onmicrosoft.com"
$SPpassword = "" 
$SPRootUrl = "https://generationreadydev.sharepoint.com"
$SPSiteUrl = "/sites/jm/workmanagement/"

$ExcelDocumentLibraryTitle = "Documents";
$ExcelDocumentName = "TestBook2.xlsx"
$ExcelDocumentTitle = "TestBook2"
$ExcelDocumentId = "745396CF-3171-485F-AF27-596F2F33EF41"
$ExcelFile = $SPurl + "/" + $ExcelDocumentLibraryTitle + "/" + $ExcelDocumentName


function Main()
{
	# get web
	$clientContext = GetSharePointContext 
	$web = $clientContext.Web
	$clientContext.Load($web)
	
	# get the document library
	$excelDocumentLibrary = $web.Lists.GetByTitle($ExcelDocumentLibraryTitle)
	$clientContext.Load($excelDocumentLibrary)

	$query = New-Object Microsoft.SharePoint.Client.CamlQuery
	$query.ViewXml = '<View><Query><Where><Eq><FieldRef Name="Title"/><Value Type="Text">' + $ExcelDocumentTitle + '</Value></Eq></Where></Query></View>'
	$excelFile = $excelDocumentLibrary.GetItems($query)
	$excelFile.RefreshLoad()
	$clientContext.Load($excelFile)
	
	$clientContext.ExecuteQuery()
	
	#UpdateExcel
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

function UpdateExcel()
{
	try
	{
		#Write-EventLog -EventId "5001" -LogName "Excel Services" -Message "Starting Excel Script" -Source "My Powershell"
	
		# Creating the excel COM Object 
		$xl = New-Object -ComObject Excel.Application; 
	
		# Setting up Excel to run without UI and without alerts
		$xl.DisplayAlerts = $false; 
		$xl.Visible = $false; 
	}
	Catch
	{
		#Write-EventLog -EventId "5001" -LogName "Excel Services" -Message "Failed to start Excel" -Source "My Powershell"
		Exit
	}

	try
	{
		# Allow update only if we can perform check out
		If ($xl.workbooks.CanCheckOut($ExcelFile))
		{
			# Opening the workbook, can be local path or SharePoint URL
			$wb = $xl.workbooks.open($ExcelFile);
			#Write-EventLog -EventId "5001" -LogName "Excel Services" -Message "Opened: $ExcelFile" -Source "My Powershell"

			# Perform the check out
			# $xl.workbooks.checkout($i)

			# Calling the refresh
			#$wb.RefreshAll();
			#Write-EventLog -EventId "5001" -LogName "Excel Services" -Message "Refreshed: $ExcelFile" -Source "My Powershell"

			# Saving and closing the workbook
			# $wb.CheckInWithVersion();

			Start-Sleep -sec 5
			#Write-EventLog -EventId "5001" -LogName "Excel Services" -Message "Slept for 5 seconds" -Source "My Powershell"

			# in case you are not using checkout/checkin perform a save and close
			#$wb.Save();
			#$wb.Close();
			#Write-EventLog -EventId "5001" -LogName "Excel Services" -Message "Saved and Closed: $ExcelFile" -Source "My Powershell"

			#Release Workbook
			[System.Runtime.Interopservices.Marshal]::ReleaseComObject($wb)
		}
		else
		{
			write-host "Check out failed for:  $ExcelFile"
			#Write-EventLog -EventId "5001" -LogName "Excel Services" -Message "Workbook can't be checked out $i" -Source "My Powershell"
		}
	}
	catch
	{
		#Write-EventLog -EventId "5001" -LogName "Excel Services" -Message "Failed refreshing the workbook $ExcelFile $_"  -Source "ExcelUpdate"    
	}
	
	#Quiting Excel
	$xl.quit(); 
	
	#Release Excel
	[System.Runtime.Interopservices.Marshal]::ReleaseComObject($xl)
}

Main
