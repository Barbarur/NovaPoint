using NovaPointLibrary.Commands.DeviceManagement;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Core.Context;

namespace NovaPointLibrary.Solutions.DeviceManagement;

public class GetManagedDevices : ISolution
{
    public static readonly string s_SolutionName = "Intune Managed Devices report";
    public static readonly string s_SolutionDocs = $"https://github.com/Barbarur/NovaPoint/wiki/Solution-{nameof(GetManagedDevices)}";

    private readonly ContextSolution _ctx;
    private readonly GetManagedDevicesParameters _param;

    private GetManagedDevices(ContextSolution context, GetManagedDevicesParameters parameters)
    {
        _ctx = context;
        _param = parameters;

        Dictionary<Type, string> solutionReports = new()
        {
            { typeof(GetManagedDevicesRecord), "Report" },
        };
        _ctx.DbHandler.AddSolutionReports(solutionReports);
    }

    public static ISolution Create(ContextSolution context, ISolutionParameters parameters)
    {
        return new GetManagedDevices(context, (GetManagedDevicesParameters)parameters);
    }

    public async Task RunAsync()
    {
        _ctx.AppClient.IsCancelled();

        string selectedProperties = """
            ?$select=
            id,
            userId,
            deviceName,
            managedDeviceOwnerType,
            managementState,
            enrolledDateTime,
            lastSyncDateTime,
            operatingSystem,
            complianceState,
            jailBroken,
            managementAgent,
            osVersion,
            azureADRegistered,
            deviceEnrollmentType,
            emailAddress,
            azureADDeviceId,
            deviceRegistrationState,
            deviceCategoryDisplayName,
            isSupervised,
            isEncrypted,
            userPrincipalName,
            model,
            manufacturer,
            serialNumber,
            complianceGracePeriodExpirationDateTime,
            phoneNumber,
            androidSecurityPatchLevel,
            userDisplayName,
            wiFiMacAddress,
            subscriberCarrier,
            meid,
            totalStorageSpaceInBytes,
            freeStorageSpaceInBytes,
            managedDeviceName,
            partnerReportedThreatState,
            managementCertificateExpirationDate,
            notes,
            ethernetMacAddress,
            physicalMemoryInBytes,
            enrollmentProfileName,
            imei
            """;

        var cmd = new MgManagedDevice(_ctx);
        var collDevices = await cmd.GetAllAsync(selectedProperties);

        ProgressTracker progress = new(_ctx.Logger, collDevices.Count());
        foreach (var device in collDevices)
        {
            GetManagedDevicesRecord record = new(device);

            try
            {
                if (_param.IncludeComplianceReasons && device.ComplianceState != "compliant")
                {
                    var collPolicyState = await cmd.GetCompliancePolicyStatesAsync(device.Id);
                    record.NoncomplianceReasons = FormatNoncomplianceReasons(collPolicyState);
                }
            }
            catch (Exception ex)
            {
                _ctx.Logger.Error(GetType().Name, "Device", device.Id, ex);
                record.Remarks = ex.Message;
            }

            AddRecord(record);
            progress.ProgressUpdateReport();
        }
    }

    private static string FormatNoncomplianceReasons(IEnumerable<GraphDeviceCompliancePolicyState> collPolicyState)
    {
        var reasons = new List<string>();
        foreach (var policyState in collPolicyState)
        {
            if (policyState.State is "compliant" or "unknown" or "notApplicable")
                continue;
            
            foreach (var setting in policyState.SettingStates)
            {
                if (setting.State is "compliant" or "unknown" or "notApplicable")
                    continue;

                // string reason = string.IsNullOrWhiteSpace(setting.ErrorDescription)
                //     ? $"{policyState.DisplayName} › {setting.SettingName}"
                //     : $"{policyState.DisplayName} › {setting.SettingName}: {setting.ErrorDescription}";
                string reason = $"{policyState.DisplayName} > {setting.Setting.Split('.').Last()}: {setting.ErrorDescription}";
                reasons.Add(reason);
            }
        }
        return string.Join(" | ", reasons);
    }

    private void AddRecord(GetManagedDevicesRecord record)
    {
        _ctx.DbHandler.WriteRecord(record);
    }
}


internal class GetManagedDevicesRecord : ISolutionRecord
{
    // Device identity
    public string DeviceName { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string AzureADDeviceId { get; set; } = string.Empty;
    public string IntuneDeviceId { get; set; } = string.Empty;
    public string DeviceCategory { get; set; } = string.Empty;

    // Hardware
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string OsVersion { get; set; } = string.Empty;
    public string TotalStorageGB { get; set; } = string.Empty;
    public string FreeStorageGB { get; set; } = string.Empty;
    public string StorageUsedPct { get; set; } = string.Empty;
    public string PhysicalMemoryGB { get; set; } = string.Empty;

    // Primary user
    public string PrimaryUser { get; set; } = string.Empty;
    public string OwnerType { get; set; } = string.Empty;

    // Management
    public string ManagementState { get; set; } = string.Empty;
    public string ManagementAgent { get; set; } = string.Empty;
    public string EnrollmentType { get; set; } = string.Empty;
    public string EnrollmentProfile { get; set; } = string.Empty;
    public string EnrolledDate { get; set; } = string.Empty;
    public string LastSyncDate { get; set; } = string.Empty;
    public string DaysSinceLastSync { get; set; } = string.Empty;
    public string ManagementCertExpiration { get; set; } = string.Empty;

    // Compliance
    public string ComplianceState { get; set; } = string.Empty;
    public string ComplianceGracePeriodExpiration { get; set; } = string.Empty;
    public string NoncomplianceReasons { get; set; } = string.Empty;

    // Security
    public string IsEncrypted { get; set; } = string.Empty;
    public string JailBroken { get; set; } = string.Empty;
    public string IsSupervised { get; set; } = string.Empty;
    public string AzureADRegistered { get; set; } = string.Empty;
    public string PartnerReportedThreatState { get; set; } = string.Empty;

    // Additional
    public string AndroidSecurityPatchLevel { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    public string Remarks { get; set; } = string.Empty;

    public GetManagedDevicesRecord() { }

    internal GetManagedDevicesRecord(GraphManagedDevice device)
    {
        DeviceName = device.DeviceName;
        SerialNumber = device.SerialNumber;
        AzureADDeviceId = device.AzureADDeviceId;
        IntuneDeviceId = device.Id;
        DeviceCategory = device.DeviceCategoryDisplayName;

        Manufacturer = device.Manufacturer;
        Model = device.Model;
        OperatingSystem = device.OperatingSystem;
        OsVersion = device.OsVersion;
        TotalStorageGB = device.TotalStorageSpaceInBytes >= 0
            ? Math.Round((double)device.TotalStorageSpaceInBytes / 1_073_741_824, 2).ToString()
            : string.Empty;
        FreeStorageGB = device.FreeStorageSpaceInBytes >= 0
            ? Math.Round((double)device.FreeStorageSpaceInBytes / 1_073_741_824, 2).ToString()
            : string.Empty;
        StorageUsedPct = device.TotalStorageSpaceInBytes > 0
            ? $"{Math.Round((double)(device.TotalStorageSpaceInBytes - device.FreeStorageSpaceInBytes) / device.TotalStorageSpaceInBytes * 100, 0)}%"
            : string.Empty;
        PhysicalMemoryGB = device.PhysicalMemoryInBytes >= 0
            ? Math.Round((double)device.PhysicalMemoryInBytes / 1_073_741_824, 2).ToString()
            : string.Empty;

        PrimaryUser = !string.IsNullOrEmpty(device.UserPrincipalName)
            ? device.UserPrincipalName
            : device.UserDisplayName;
        OwnerType = NormalizeOwnerType(device.ManagedDeviceOwnerType);

        ManagementState = NormalizeManagementState(device.ManagementState);
        ManagementAgent = NormalizeManagementAgent(device.ManagementAgent);
        EnrollmentType = NormalizeEnrollmentType(device.DeviceEnrollmentType);
        EnrollmentProfile = device.EnrollmentProfileName;
        EnrolledDate = device.EnrolledDateTime != DateTime.MinValue
            ? device.EnrolledDateTime.ToString("yyyy-MM-dd HH:mm:ss")
            : string.Empty;
        LastSyncDate = device.LastSyncDateTime != DateTime.MinValue
            ? device.LastSyncDateTime.ToString("yyyy-MM-dd HH:mm:ss")
            : string.Empty;
        DaysSinceLastSync = device.LastSyncDateTime != DateTime.MinValue
            ? ((int)(DateTime.UtcNow - device.LastSyncDateTime).TotalDays).ToString()
            : string.Empty;
        if (device.ManagementCertificateExpirationDate == DateTime.MinValue)
            ManagementCertExpiration = "None";
        else if (device.ManagementCertificateExpirationDate < DateTime.UtcNow)
            ManagementCertExpiration = "Expired";
        else if (device.ManagementCertificateExpirationDate < DateTime.UtcNow.AddDays(30))
            ManagementCertExpiration = $"Expiring {device.ManagementCertificateExpirationDate:yyyy-MM-dd}";
        else
            ManagementCertExpiration = "Valid";

        ComplianceState = NormalizeComplianceState(device.ComplianceState);
        ComplianceGracePeriodExpiration = device.ComplianceGracePeriodExpirationDateTime != DateTime.MinValue
            && device.ComplianceGracePeriodExpirationDateTime.Year < 9999
            ? device.ComplianceGracePeriodExpirationDateTime.ToString("yyyy-MM-dd HH:mm:ss")
            : string.Empty;

        IsEncrypted = device.IsEncrypted?.ToString() ?? string.Empty;
        JailBroken = NormalizeJailBroken(device.JailBroken);
        IsSupervised = device.IsSupervised?.ToString() ?? string.Empty;
        AzureADRegistered = device.AzureADRegistered?.ToString() ?? string.Empty;
        PartnerReportedThreatState = NormalizeThreatState(device.PartnerReportedThreatState);

        AndroidSecurityPatchLevel = device.AndroidSecurityPatchLevel;
        Notes = device.Notes;
    }

    private static string NormalizeComplianceState(string s) => s switch
    {
        "compliant"     => "Compliant",
        "noncompliant"  => "Non-Compliant",
        "inGracePeriod" => "In Grace Period",
        "error"         => "Error",
        "unknown"       => "Unknown",
        "notApplicable" => "Not Applicable",
        _               => s,
    };

    private static string NormalizeManagementState(string s) => s switch
    {
        "managed"         => "Managed",
        "retirePending"   => "Retire Pending",
        "retireFailed"    => "Retire Failed",
        "wipePending"     => "Wipe Pending",
        "wipeFailed"      => "Wipe Failed",
        "unhealthy"       => "Unhealthy",
        "deletePending"   => "Delete Pending",
        "retireIssued"    => "Retire Issued",
        "wipeIssued"      => "Wipe Issued",
        "wipeCancelled"   => "Wipe Cancelled",
        "retireCancelled" => "Retire Cancelled",
        "discovered"      => "Discovered",
        _                 => s,
    };

    private static string NormalizeManagementAgent(string s) => s switch
    {
        "eas"                               => "EAS",
        "mdm"                               => "MDM",
        "easMdm"                            => "EAS + MDM",
        "intuneClient"                      => "Intune Client",
        "easIntuneClient"                   => "EAS + Intune Client",
        "configurationManager"              => "Configuration Manager",
        "unknown"                           => "Unknown",
        "jamf"                              => "Jamf",
        "googleCloudDevicePolicyController" => "Google Cloud DPC",
        _                                   => s,
    };

    private static string NormalizeOwnerType(string s) => s switch
    {
        "company"  => "Company",
        "personal" => "Personal",
        "unknown"  => "Unknown",
        _          => s,
    };

    private static string NormalizeJailBroken(string s) => s switch
    {
        "Unknown"       => "Unknown",
        "NotJailbroken" => "Not Jailbroken",
        "Jailbroken"    => "Jailbroken",
        _               => s,
    };

    private static string NormalizeThreatState(string s) => s switch
    {
        "unknown"        => "Unknown",
        "activated"      => "Activated",
        "deactivated"    => "Deactivated",
        "secured"        => "Secured",
        "lowSeverity"    => "Low Severity",
        "mediumSeverity" => "Medium Severity",
        "highSeverity"   => "High Severity",
        "unresponsive"   => "Unresponsive",
        "compromised"    => "Compromised",
        "misconfigured"  => "Misconfigured",
        _                => s,
    };

    private static string NormalizeEnrollmentType(string s) => s switch
    {
        "unknown"                             => "Unknown",
        "userEnrollment"                      => "User Enrollment",
        "deviceEnrollmentManager"             => "Device Enrollment Manager",
        "appleBulkWithUser"                   => "Apple Bulk (with User)",
        "appleBulkWithoutUser"                => "Apple Bulk (without User)",
        "windowsAzureADJoin"                  => "Windows Azure AD Join",
        "windowsBulkUserless"                 => "Windows Bulk (Userless)",
        "windowsAutoEnrollment"               => "Windows Auto Enrollment",
        "windowsBulkAzureDomainJoin"          => "Windows Bulk Azure Domain Join",
        "windowsCoManagement"                 => "Windows Co-Management",
        "windowsAzureADJoinUsingDeviceTenant" => "Windows Azure AD Join (Device Tenant)",
        _                                     => s,
    };
}


public class GetManagedDevicesParameters : ISolutionParameters
{
    public bool IncludeComplianceReasons { get; set; } = true;
}
