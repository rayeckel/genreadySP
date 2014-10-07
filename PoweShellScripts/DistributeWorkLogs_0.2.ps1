<#
.SYNOPSIS
   Gets all entries from the "Project Update Forms" form library that have
   been marked as "Accepted", but no "email" date is present. (Which indicates that
   the person has not yet been informed, and that the final project form .PDF has not been persisted)
   
   Fetches the final version of the project update form .PDF from ReadyPath and stores it to SharePoint
   
   Sends out emails to everyone in the "Distribution" table that is related either by District ID or School Id
   
.DESCRIPTION
   <A detailed description of the script>
.PARAMETER <paramName>
   <Description of script parameter>
.EXAMPLE
   <An example of using the script>
#>


#TODO: add error handling


# load in sharepoint classes 
# the path here may need to change if you used e.g. C:\Lib.. 
# note that you might need some other references (depending on what your script does):
Add-Type -Path "c:\Program Files\Common Files\Microsoft Shared\Web Server Extensions\15\ISAPI\Microsoft.SharePoint.Client.dll" 
Add-Type -Path "c:\Program Files\Common Files\Microsoft Shared\Web Server Extensions\15\ISAPI\Microsoft.SharePoint.Client.Runtime.dll" 
Add-Type -Path "c:\Program Files\Common Files\Microsoft Shared\Web Server Extensions\15\ISAPI\Microsoft.SharePoint.Client.Taxonomy.dll" 

# SP context info (consider using Get-Credential to enter password securely as script runs).. 
$SPurl = "https://generationreadydev.sharepoint.com/sites/jm/workmanagement"
$SPusername = "ray.eckel@generationreadydev.onmicrosoft.com"
$SPpassword = "" 
$SPRootUrl = "https://generationreadydev.sharepoint.com"
$SPSiteUrl = "/sites/jm/workmanagement/"

# set titles for libraries and properties we'll be working with
$projectUpdateFormLibraryTitle = "Project Update Forms"
$workLogLibraryTitle = "Work Logs"
$workLogLibraryTitleURLEncoded = "Work%20Logs"
$distributionListTitle = "Distribution"
$engagementDateTitle = "Engagement_x0020_Date"
$articleIdTitle = "Article_x0020_Id"
$articleListTitle = "Articles"
$workLogIdTitle = "RPWorkLogId"
$altWorkLogIdTitle = "WorkLogId"
$customerIdTitle = "Customer_x0020_Id"
$schoolIdTitle = "School_x0020_Id"

#email parameters
$smtpUsername = "rayeckel@gmail.com"
$smtpPassword = "" #we'll need to encrypy and store this approrpiately
$smtpServer = 'smtp.gmail.com'
$from = 'clientservices@generationready.com'
$bcc = 'johnnie.margerison@generationready.com'
$replyTo = 'clientservices@generationready.com'

#other parameters
$workLogsPDFBaseUrl = "http://readypath.generationready.com/sauth/worklogs/pdf/"

function Main()
{
	# get web
	$clientContext = GetSharePointContext 
	$web = $clientContext.Web
	$clientContext.Load($web)
	
	# get the project update form and work log libraries
	$projectUpdateFormLibrary = $web.Lists.GetByTitle($projectUpdateFormLibraryTitle)
	$clientContext.Load($projectUpdateFormLibrary)
	$workLogLibrary = $web.Lists.GetByTitle($workLogLibraryTitle)
	$clientContext.Load($workLogLibrary)
	$articleList = $web.Lists.GetByTitle($articleListTitle)
	$clientContext.Load($articleList)
	$distributionList = $web.Lists.GetByTitle($distributionListTitle)
	$clientContext.Load($distributionList)
	
	# set the query to pull any approved but unsent work logs from the project update form library
	# we only need to get the customerid, school id, work log id and item id from here, set query limits
	$query = New-Object Microsoft.SharePoint.Client.CamlQuery
	$query.ViewXml = "<View><Query> " +
					"<ViewFields><FieldRef Name='Status' /><FieldRef Name='" + $articleIdTitle + "' /><FieldRef Name='" + $engagementDateTitle + "' /><FieldRef Name='" + $workLogIdTitle + "' /></ViewFields>" +
					"<Where><And><IsNull><FieldRef Name='Sent'/></IsNull><Eq><FieldRef Name='Status'/><Value Type='Text'>Approved</Value></Eq></And></Where></Query></View>"
	$items = $projectUpdateFormLibrary.GetItems($query)
	$clientContext.Load($items)
	
	$clientContext.ExecuteQuery()

	# iterate through items from project update form library
	for($i=0;$i -lt $items.Count; $i++)
	{
		$item = $items[$i]
	
		#get customer information from article list
		$articleId = $item[$articleIdTitle]
		$query.ViewXml = '<View><Query><Where><Eq><FieldRef Name="' + $articleIdTitle + '"/><Value Type="Text">' + $articleId + '</Value></Eq></And></Where></Query></View>'
		$articleItems = $articleList.GetItems($query)
		$clientContext.Load($articleItems)
		
		$clientContext.ExecuteQuery()
		
		$articleItem = $articleItems[0]
		$clientContext.Load($articleItem)
		
		$clientContext.ExecuteQuery()
		
		#get recipients based on distribution list
		$customerId = $articleItem[$customerIdTitle]
		$schoolId = $articleItem[$schoolIdTitle]
		$query.ViewXml = '<View><Query><Where><Or><And><Eq><FieldRef Name="CustomerId"/><Value Type="Number">' + $customerId + '</Value></Eq>' +
			'<Eq><FieldRef Name="SchoolId"/><Value Type="Text">' + $schoolId + '</Value></Eq></And>' +
			'<And><IsNull><FieldRef Name="SchoolId"/></IsNull>' +
			'<Eq><FieldRef Name="CustomerId"/><Value Type="Number">' + $customerId + '</Value></Eq></And></Or></Where></Query></View>'
		$distributionItems = $distributionList.GetItems($query)
		$clientContext.Load($distributionItems)
		
		$clientContext.ExecuteQuery()
		
		#get the list of email recipients
		$recipientsArray = GetDistributionList $distributionItems
		#$reportSet = BuildReportSet $distributionItems
		#GenerateExcelOutput $reportSet
		
		#get the pdf from readypath
		$rpWorkLogId = $item[$workLogIdTitle]
		$fileStream = FetchPDF $rpWorkLogId
		
		#send email
		$emailSendResult = SendEmail $recipientsArray $item $fileStream
			
		if($emailSendResult) {
		
			#upload to the work logs library as the approved and delivered version
			#add metadata, allocation id, engagement data, consultant, work log id
			
			$filename = $emailSendResult
			
			#set the sent field on email delivered
			$item['Sent'] = Get-Date
			$item.Update()
			
			#save the approved report to SP
			$fileCreationInfo = New-Object Microsoft.SharePoint.Client.FileCreationInformation
			$fileCreationInfo.Overwrite = $true
			$fileStream.Position = 0
			$fileCreationInfo.Content = $fileStream.ToArray()
			$fileCreationInfo.Url = $SPRootUrl + $SPSiteUrl + $workLogLibraryTitleURLEncoded + "/" + $filename
			$uploadFile = $workLogLibrary.RootFolder.Files.Add($fileCreationInfo)
			
			#Add some metadata
			$splitFileName = $fileName.Split(".")
			$uploadFile.ListItemAllFields[$altWorkLogIdTitle] = $splitFileName[0]
			$uploadFile.ListItemAllFields.Update()

			$clientContext.ExecuteQuery()
		}

		#clean up
		$fileStream.Flush()
		$fileStream.Close()
		$fileStream.Dispose()
	}
	
	#clean up
	$clientContext.Dispose()
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

function FetchPDF
{ 
	param([string]$workLogId) 
	
	$sourceFileUrl = $workLogsPDFBaseUrl + $workLogId
	$requestParams = @{ "docName" = $workLogId; "param2" = "pdf" }
	
	$hashedRequestParams = BuildPutRequestHash $requestParams
	$mySourceUrl = GenerateSecureParamUrl $sourceFileUrl $hashedRequestParams
	
	$fileStream = DownloadFile $mySourceUrl
	
	return $fileStream
}

function DownloadFile
{ 
	param([string]$sourceFileUrl) 

    $sourceUri = New-Object System.Uri $sourceFileUrl 
    $request = [System.Net.HttpWebRequest]::Create($sourceUri) 
    $request.set_Timeout(15000) #15 second timeout 
	
	#Write-Host "Calling PDF host site: '$sourceUri'" -ForegroundColor Green 
	
    $response = $request.GetResponse() 
    $totalLength = [System.Math]::Floor($response.get_ContentLength()/1024) 
    $responseStream = $response.GetResponseStream() 

	$fileStreamBuffer =  new-object byte[] 10KB
	$fileStream = New-Object System.IO.MemoryStream
	
    $count = $responseStream.Read($fileStreamBuffer,0,$fileStreamBuffer.length)
	$downloadedBytes = $count 
 	while ($count -gt 0) 
    { 
        #[System.Console]::CursorLeft = 0 
        #[System.Console]::Write("Downloaded {0}K of {1}K", [System.Math]::Floor($downloadedBytes/1024), $totalLength)
		
        $fileStream.Write($fileStreamBuffer, 0, $count) 
        $count = $responseStream.Read($fileStreamBuffer,0,$fileStreamBuffer.length) 
        $downloadedBytes = $downloadedBytes + $count 
    } 
	
    $responseStream.Dispose()
	$fileStream.Position = 0
	
	return $fileStream
}

function GenerateSecureParamUrl
{
	param([string]$sourceFileUrl, [hashtable]$requestParams)
	
    $sourceFileUrl += "?";
	
	$requestParams.GetEnumerator() | % { 
		$appendString = "&" + $_.key + "=" + $_.value
        $sourceFileUrl += $appendString
	}
	
	return $sourceFileUrl
}

function BuildPutRequestHash
{
	param([hashtable]$requestParams)

    #Generate a nonce by appending a random number to the string representation of now()
    $randomNumber = Get-Random -Minimum 10000 -Maximum 99999
    $unixTimestamp = get-date -uformat %s
    $nonce = [string]::Format("{0}{1}", $unixTimestamp, $randomNumber)
    $requestParams.nonce = $nonce

    #Acquire dictionary keys and sort them.
	$sortedRequestParams = $requestParams.GetEnumerator() | Sort-Object name

    $sb = "secrete123"
	$requestParams.GetEnumerator() | % {
		$sb += $_.value
	}
	
	$hash = Get-Hash -Algorithm "MD5" -Text $sb
    $requestParams.hash = $hash;
	
	return $requestParams
}

function GetDistributionList
{
	param([Microsoft.Sharepoint.Client.ListItemCollection]$distributionItems)
	$recipientsArray = @()

	#iterate through distribution list items and get recipients
	for($j=0; $j -lt $distributionItems.Count; $j++)
	{
		$distributionItem = $distributionItems[$j]
		$recipientsArray += $distributionItem['Email']
	}
	
	return $recipientsArray
}

function BuildReportSet
{
	param([Microsoft.Sharepoint.Client.ListItemCollection]$distributionItems)
	$reportSet = @()
	
	for($j=0; $j -lt $distributionItems.Count; $j++)
	{
		$distributionItem = $distributionItems[$j]
		$reportProperties = @{
								Email = $distributionItem['Email']
								FirstName = $distributionItem['FirstName']
								LastName = $distributionItem['LastName']
								CustomerId = $distributionItem['CustomerId']
								SchoolId = $distributionItem['SchoolId']
								WorkLogId = $workLogId
							}
		$reportObject = New-Object PSObject -Property $reportProperties
		$reportSet += $reportObject
	}
	
	return $reportSet
}

function SendEmail
{
	param([string[]]$recipientsArray, [Microsoft.Sharepoint.Client.ListItem] $item, [System.IO.MemoryStream]$fileStream) 
	
	$contentType = "application/pdf"
	$fileNameExtension = ".pdf"
	$fileNameRoot = [string]$item[$workLogIdTitle]
	$fileName = $fileNameRoot + $fileNameExtension
	$attachment = New-Object System.Net.Mail.Attachment($fileStream, $fileName, $contentType)
	
	$recipients = [string]::Join(',',$recipientsArray)
	$engagementDate = $item[$engagementDateTitle]
	$engagementDateString = $engagementDate.ToShortDateString()												
	$subject = 'Generation Ready Work Log for ' + $engagementDateString
	$body = 'Please find attached the end of project report from Generation Ready for work completed on ' + $engagementDateString + ". `r`n" +
		'If you have any questions or require assistance please contact your Generation Ready representative directly or reply to this email, at clientservices@generationready.com.'

	$mailMessage = New-Object Net.Mail.MailMessage
	$mailMessage.From = $from
	$mailMessage.To.Add($recipients)
	$mailMessage.Bcc.Add($Bcc)
	$mailMessage.Subject = $subject 
	$mailMessage.Body = $body
	$mailMessage.IsBodyHtml = $false
	$mailMessage.Attachments.Add($attachment)
	$mailMessage.ReplyTo = $replyTo
		
	#email config
	$smtpClient = New-Object Net.Mail.SmtpClient($smtpServer, 587)
	$smtpClient.EnableSsl = $true
	$smtpClient.Credentials = New-Object System.Net.NetworkCredential($smtpUsername, $smtpPassword) 
	
	if ($smtpClient.SendMailAsync($mailMessage)) {
		return $fileName
	} else {
		return false;
	}
}

function GenerateExcelOutput
{
	param([Object[]]$reportSet)
	
	#add to excel output for reports delivered / undelivered and upload to sharepoint log library
	#$report | Out-GridView
	$Path = "$env:temp\$(Get-Date -Format yyyyMMddHHmmss).csv"
	$reportSet | Export-CSV -Path $Path -UseCulture -Encoding UTF8 -NoTypeInformation
	Invoke-Item -Path $Path
}

function Get-Hash
{
    Param
    (
        [parameter(Mandatory=$true, ValueFromPipeline=$true, ParameterSetName="set1")]
        [String]
        $text,
        [parameter(Position=0, Mandatory=$true, ValueFromPipeline=$false, ParameterSetName="set2")]
        [String]
        $file = "",
        [parameter(Mandatory=$false, ValueFromPipeline=$false)]
        [ValidateSet("MD5", "SHA", "SHA1", "SHA-256", "SHA-384", "SHA-512")]
        [String]
        $algorithm = "SHA1"
    )

    Begin
    {
        $hashAlgorithm = [System.Security.Cryptography.HashAlgorithm]::Create($algorithm)
    }
	Process
	{
        $md5StringBuilder = New-Object System.Text.StringBuilder 50
        $ue = New-Object System.Text.UTF8Encoding 

        if ($file){
            try {
                if (!(Test-Path -literalpath $file)){
                    throw "Test-Path returned false."
                }
            }
            catch {
                throw "Get-Hash - File not found or without permisions: [$file]. $_"
            } 

            try {        
                [System.IO.FileStream]$fileStream = [System.IO.File]::Open($file, [System.IO.FileMode]::Open);
                $hashAlgorithm.ComputeHash($fileStream) | % { [void] $md5StringBuilder.Append($_.ToString("x2")) }
            }
            catch {
                throw "Get-Hash - Error reading or hashing the file: [$file]"
            } 
            finally {
                $fileStream.Close()
                $fileStream.Dispose()
            }
        }
        else {
            $hashAlgorithm.ComputeHash($ue.GetBytes($text)) | % { [void] $md5StringBuilder.Append($_.ToString("x2")) }
        }
        
        return $md5StringBuilder.ToString()
    }
}

Main
