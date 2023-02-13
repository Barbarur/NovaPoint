#################################################################
# DEFINE PARAMETERS FOR THE CASE
#################################################################
$AdminSiteURL = "https://<DOMAIN>-admin.sharepoint.com" # SharePoint Admin Center Url
$SiteUserAccess = "https://<DOMAIN>.sharepoint.com/sites/<SITENAME>" # Site Url where user has correct access
$SiteCollAdmin = "<ADMIN@EMAIL.com>" # Global or SharePoint Admin used to run the script.
$AffectedUser = "<AFFECTEDUSER@EMAIL.com>" # Email of the affected user.
$FolderPath = "$Env:USERPROFILE\Documents\" # Location where to generate the report



#################################################################
# REPORT AND LOGS FUNCTIONS
#################################################################
# Add new record on the report
Function Add-ReportRecord($SiteURL, $Action)
{
    $Record = New-Object PSObject -Property ([ordered]@{
        "Site URL"          = $SiteURL
        "Action"            = $Action
        })
    
    $Record | Export-Csv -Path $ReportOutput -NoTypeInformation -Append
}

# Add Log of the Script
Function Add-ScriptLog($Color, $Msg)
{
    $Date = Get-Date -Format "yyyy/MM/dd HH:mm"
    $Msg = $Date + " - " + $Msg
    Add-Content -Path $LogsOutput -Value $Msg
    Write-host -f $Color $Msg
}

# Create Report location
$Date = Get-Date -Format "yyyyMMdd_HHmmss"
$ReportName = "UserIDMismatchReport"
$FolderPath = $FolderPath + "Script_Output\"
$FolderName = $Date + "_" + $ReportName
New-Item -Path $FolderPath -Name $FolderName -ItemType "directory"
$ReportOutput = $FolderPath + $FolderName + "\" + $ReportName + ".csv"

# Create logs file
$LogsName = $ReportName + "_Logs.txt"
$LogsOutput = $FolderPath + $FolderName + "\" + $LogsName

Add-ScriptLog -Color Cyan -Msg "Report will be generated at $($ReportOutput)"



#################################################################
# SCRIPT LOGIC
#################################################################
function Remove-UserIDMismatch ($SiteUrl) {
    try {
        Connect-PnPOnline -Url $Site.Url -Interactive -ErrorAction Stop
        $User = Get-PnPUser | Where-Object { $_.Email -eq $AffectedUser -and $_.UserId.NameId -ne $UserID }
        
        If ($User.Length -eq 0) {
            Add-ScriptLog -Color Yellow -Msg "$($PercentComplete)% Completed - Checking Site: $($Site.Title) - User with incorrect SharePoint ID not found on the site."
        }
        Else {
            Add-ScriptLog -Color Yellow -Msg "$($PercentComplete)% Completed - Checking Site: $($Site.Title) - User with incorrect SharePoint ID $($Site.UserId.NameId) found on the site."
            
            if($User.IsSiteAdmin) {
                Remove-PnPSiteCollectionAdmin -Owners $AffectedUser -ErrorAction Stop
                Add-ScriptLog -Color Yellow -Msg "$($PercentComplete)% Completed - Checking Site: $($Site.Title) - User removed as Site Collection Admin."
                Add-ReportRecord -SiteURL $Site.Url -Action "Removed user as Site Collection Admin"
            }

            Remove-PnPUser -Identity $User.ID -Force -ErrorAction Stop
            Add-ScriptLog -Color Yellow -Msg "$($PercentComplete)% Completed - Checking Site: $($Site.Title) - User removed from Site Collection."
            Add-ReportRecord -SiteURL $Site.Url -Action "Removed user from Site"
        }
    }
    catch {
        throw
    }
}


try {
    Connect-PnPOnline -Url $AdminSiteURL -Interactive -ErrorAction Stop
    $SitesList = Get-PnPTenantSite -IncludeOneDriveSites | Where-Object { ($_.Title -notlike "" -and $_.Template -notlike "*Redirect*") }
    Add-ScriptLog -Color Cyan -Msg "Connected to SharePoint Admin Center and Collected all Site Collections"
}
catch {
    Add-ScriptLog -Color Red -Msg "Error: $($_.Exception.Message)"
    break
}

# Get correct User SharePoint ID
try {
    Set-PnPTenantSite -Url $SiteUserAccess -Owners $SiteCollAdmin -ErrorAction Stop
    Connect-PnPOnline -Url $SiteUserAccess -Interactive -ErrorAction Stop
    $UserProfile = Get-PnPUser | Where-Object { $_.Email -eq $AffectedUser}
    $UserID = $UserProfile.UserId.NameId
    Add-ScriptLog -Color Cyan -Msg 'User correct ID: $UserID'

}
catch {
    Add-ScriptLog -Color Red -Msg "Error: $($_.Exception.Message)"
    break
}

# Iterate through the Sites
$ItemCounter = 0
ForEach($Site in $SitesList) {

    # Adding notification and logs
    $PercentComplete = [math]::Round($ItemCounter/$SitesList.Count * 100, 2)
    Add-ScriptLog -Color Yellow -Msg "$($PercentComplete)% Completed - Checking Site: $($Site.Title)"
    $ItemCounter++

    Set-PnPTenantSite -Url $Site.Url -Owners $SiteCollAdmin
    
    Try {
        # Check if user has ID mismatch on target Site Collection and remove if needed
        Remove-UserIDMismatch -SiteUrl $Site.Url -ErrorAction Stop
        
        # Collect all Subsites and iterate through method to remove user with ID mismatch
        $SubSites = Get-PnPSubWeb -Recurse
        ForEach($Site in $SubSites) {
            Remove-UserIDMismatch -SiteUrl $Site.Url -ErrorAction Stop
        }
    }
    Catch {
        Add-ScriptLog -Color Red -Msg "Error when processing Site '$($Site.Url)'"
        Add-ScriptLog -Color Red -Msg $_.Exception.Message
        Add-ReportRecord -SiteURL $Site.Url -Action $_.Exception.Message
    }

    Remove-PnPSiteCollectionAdmin -Owners $SiteCollAdmin
}
# Close status notification
if($ItemsList.Count -ne 0) { 
    $PercentComplete = [math]::Round($ItemCounter/$ItemsList.Count * 100, 1) 
    Add-ScriptLog -Color Cyan -Msg "$($PercentComplete)% Completed - Finished running script"
}
Add-ScriptLog -Color Cyan -Msg "Report generated at at $($ReportOutput)"
