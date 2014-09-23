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


try
{

    Write-EventLog -EventId "5001" -LogName "Excel Services" -Message "Starting Excel Script" -Source "My Powershell"

    # Creating the excel COM Object 
    $xl = New-Object -ComObject Excel.Application; 

    # Setting up Excel to run without UI and without alerts
    $xl.DisplayAlerts = $false; 
    $xl.Visible = $false; 
}
Catch
{
    Write-EventLog -EventId "5001" -LogName "Excel Services" -Message "Failed to start Excel" -Source "My Powershell"
    Exit
}

foreach ($i in $args)
{

    write-host "handling $i"
    try
    {
        # Allow update only if we can perform check out
        If ($xl.workbooks.CanCheckOut($i))
        {

            # Opening the workbook, can be local path or SharePoint URL
            $wb = $xl.workbooks.open($i);
            Write-EventLog -EventId "5001" -LogName "Excel Services" -Message "Opened: $i" -Source "My Powershell"

            # Perform the check out
            # $xl.workbooks.checkout($i)

            # Calling the refresh
            $wb.RefreshAll();
            Write-EventLog -EventId "5001" -LogName "Excel Services" -Message "Refreshed: $i" -Source "My Powershell"

            # Saving and closing the workbook
            # $wb.CheckInWithVersion();

            Start-Sleep -sec 5
            Write-EventLog -EventId "5001" -LogName "Excel Services" -Message "Slept for 5 seconds" -Source "My Powershell"

            # in case you are not using checkout/checkin perform a save and close
            $wb.Save();
            $wb.Close();
            Write-EventLog -EventId "5001" -LogName "Excel Services" -Message "Saved and Closed: $i" -Source "My Powershell"

            #Release Workbook
            [System.Runtime.Interopservices.Marshal]::ReleaseComObject($wb)
        }
        else
        {
            write-host "Check out failed for:  $i"
            Write-EventLog -EventId "5001" -LogName "Excel Services" -Message "Workbook can't be checked out $i" -Source "My Powershell"
        }
    }
    catch
    {
        Write-EventLog -EventId "5001" -LogName "Excel Services" -Message "Failed refreshing the workbook $i $_" -Source "My Powershell"        
    }
}

#Quiting Excel
$xl.quit(); 

#Release Excel
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($xl)
