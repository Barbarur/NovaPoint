using Newtonsoft.Json;

namespace NovaPointLibrary.Commands.Utilities.GraphModel;

public class GraphManagedDevice
{
    [JsonProperty("@odata.type")]
    public string OdataType { get; set; } = string.Empty;
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;
    [JsonProperty("userId")]
    public string UserId { get; set; } = string.Empty;
    [JsonProperty("deviceName")]
    public string DeviceName { get; set; } = string.Empty;
    [JsonProperty("managedDeviceOwnerType")]
    public string ManagedDeviceOwnerType { get; set; } = string.Empty;
    [JsonProperty("deviceActionResults")]
    public List<DeviceActionResult> DeviceActionResults { get; set; } = [];
    [JsonProperty("managementState")]
    public string ManagementState { get; set; } = string.Empty;
    [JsonProperty("enrolledDateTime")]
    public DateTime EnrolledDateTime { get; set; } = DateTime.MinValue;
    [JsonProperty("lastSyncDateTime")]
    public DateTime LastSyncDateTime { get; set; } = DateTime.MinValue;
    [JsonProperty("operatingSystem")]
    public string OperatingSystem { get; set; } = string.Empty;
    [JsonProperty("complianceState")]
    public string ComplianceState { get; set; } = string.Empty;
    [JsonProperty("jailBroken")]
    public string JailBroken { get; set; } = string.Empty;
    [JsonProperty("managementAgent")]
    public string ManagementAgent { get; set; } = string.Empty;
    [JsonProperty("osVersion")]
    public string OsVersion { get; set; } = string.Empty;
    [JsonProperty("easActivated")]
    public bool EasActivated { get; set; } = false;
    [JsonProperty("easDeviceId")]
    public string EasDeviceId { get; set; } = string.Empty;
    [JsonProperty("easActivationDateTime")]
    public DateTime EasActivationDateTime { get; set; } = DateTime.MinValue;
    [JsonProperty("azureADRegistered")]
    public bool AzureADRegistered { get; set; } = false;
    [JsonProperty("deviceEnrollmentType")]
    public string DeviceEnrollmentType { get; set; } = string.Empty;
    [JsonProperty("activationLockBypassCode")]
    public string ActivationLockBypassCode { get; set; } = string.Empty;
    [JsonProperty("emailAddress")]
    public string EmailAddress { get; set; } = string.Empty;
    [JsonProperty("azureADDeviceId")]
    public string AzureADDeviceId { get; set; } = string.Empty;
    [JsonProperty("deviceRegistrationState")]
    public string DeviceRegistrationState { get; set; } = string.Empty;
    [JsonProperty("deviceCategoryDisplayName")]
    public string DeviceCategoryDisplayName { get; set; } = string.Empty;
    [JsonProperty("isSupervised")]
    public bool IsSupervised { get; set; } = false;
    [JsonProperty("exchangeLastSuccessfulSyncDateTime")]
    public DateTime ExchangeLastSuccessfulSyncDateTime { get; set; } = DateTime.MinValue;
    [JsonProperty("exchangeAccessState")]
    public string ExchangeAccessState { get; set; } = string.Empty;
    [JsonProperty("exchangeAccessStateReason")]
    public string ExchangeAccessStateReason { get; set; } = string.Empty;
    [JsonProperty("remoteAssistanceSessionUrl")]
    public string RemoteAssistanceSessionUrl { get; set; } = string.Empty;
    [JsonProperty("remoteAssistanceSessionErrorDetails")]
    public string RemoteAssistanceSessionErrorDetails { get; set; } = string.Empty;
    [JsonProperty("isEncrypted")]
    public bool IsEncrypted { get; set; } = false;
    [JsonProperty("userPrincipalName")]
    public string UserPrincipalName { get; set; } = string.Empty;
    [JsonProperty("model")]
    public string Model { get; set; } = string.Empty;
    [JsonProperty("manufacturer")]
    public string Manufacturer { get; set; } = string.Empty;
    [JsonProperty("imei")]
    public string Imei { get; set; } = string.Empty;
    [JsonProperty("complianceGracePeriodExpirationDateTime")]
    public DateTime ComplianceGracePeriodExpirationDateTime { get; set; } = DateTime.MinValue;
    [JsonProperty("serialNumber")]
    public string SerialNumber { get; set; } = string.Empty;
    [JsonProperty("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;
    [JsonProperty("androidSecurityPatchLevel")]
    public string AndroidSecurityPatchLevel { get; set; } = string.Empty;
    [JsonProperty("userDisplayName")]
    public string UserDisplayName { get; set; } = string.Empty;
    [JsonProperty("configurationManagerClientEnabledFeatures")]
    public ConfigurationManagerClientEnabledFeatures ConfigurationManagerClientEnabledFeatures { get; set; } = new();
    [JsonProperty("wiFiMacAddress")]
    public string WiFiMacAddress { get; set; } = string.Empty;
    [JsonProperty("deviceHealthAttestationState")]
    public DeviceHealthAttestationState DeviceHealthAttestationState { get; set; } = new();
    [JsonProperty("subscriberCarrier")]
    public string SubscriberCarrier { get; set; } = string.Empty;
    [JsonProperty("meid")]
    public string Meid { get; set; } = string.Empty;
    [JsonProperty("totalStorageSpaceInBytes")]
    public int TotalStorageSpaceInBytes { get; set; } = -1;
    [JsonProperty("freeStorageSpaceInBytes")]
    public int FreeStorageSpaceInBytes { get; set; } = -1;
    [JsonProperty("managedDeviceName")]
    public string ManagedDeviceName { get; set; } = string.Empty;
    [JsonProperty("partnerReportedThreatState")]
    public string PartnerReportedThreatState { get; set; } = string.Empty;
    [JsonProperty("requireUserEnrollmentApproval")]
    public bool RequireUserEnrollmentApproval { get; set; } = false;
    [JsonProperty("managementCertificateExpirationDate")]
    public DateTime ManagementCertificateExpirationDate { get; set; } = DateTime.MinValue;
    [JsonProperty("iccid")]
    public string Iccid { get; set; } = string.Empty;
    [JsonProperty("udid")]
    public string Udid { get; set; } = string.Empty;
    [JsonProperty("notes")]
    public string Notes { get; set; } = string.Empty;
    [JsonProperty("ethernetMacAddress")]
    public string EthernetMacAddress { get; set; } = string.Empty;
    [JsonProperty("physicalMemoryInBytes")]
    public int PhysicalMemoryInBytes { get; set; } = -1;
    [JsonProperty("enrollmentProfileName")]
    public string EnrollmentProfileName { get; set; } = string.Empty;
}

public class ConfigurationManagerClientEnabledFeatures
{
    [JsonProperty("@odata.type")]
    public string OdataType { get; set; } = string.Empty;
    [JsonProperty("inventory")]
    public bool Inventory { get; set; } = false;
    [JsonProperty("modernApps")]
    public bool ModernApps { get; set; } = false;
    [JsonProperty("resourceAccess")]
    public bool ResourceAccess { get; set; } = false;
    [JsonProperty("deviceConfiguration")]
    public bool DeviceConfiguration { get; set; } = false;
    [JsonProperty("compliancePolicy")]
    public bool CompliancePolicy { get; set; } = false;
    [JsonProperty("windowsUpdateForBusiness")]
    public bool WindowsUpdateForBusiness { get; set; } = false;
}

public class DeviceActionResult
{
    [JsonProperty("@odata.type")]
    public string OdataType { get; set; } = string.Empty;
    [JsonProperty("actionName")]
    public string ActionName { get; set; } = string.Empty;
    [JsonProperty("actionState")]
    public string ActionState { get; set; } = string.Empty;
    [JsonProperty("startDateTime")]
    public DateTime StartDateTime { get; set; } = DateTime.MinValue;
    [JsonProperty("lastUpdatedDateTime")]
    public DateTime LastUpdatedDateTime { get; set; } = DateTime.MinValue;
}

public class DeviceHealthAttestationState
{
    [JsonProperty("@odata.type")]
    public string OdataType { get; set; } = string.Empty;
    [JsonProperty("lastUpdateDateTime")]
    public string LastUpdateDateTime { get; set; } = string.Empty;
    [JsonProperty("contentNamespaceUrl")]
    public string ContentNamespaceUrl { get; set; } = string.Empty;
    [JsonProperty("deviceHealthAttestationStatus")]
    public string DeviceHealthAttestationStatus { get; set; } = string.Empty;
    [JsonProperty("contentVersion")]
    public string ContentVersion { get; set; } = string.Empty;
    [JsonProperty("issuedDateTime")]
    public DateTime IssuedDateTime { get; set; } = DateTime.MinValue;
    [JsonProperty("attestationIdentityKey")]
    public string AttestationIdentityKey { get; set; } = string.Empty;
    [JsonProperty("resetCount")]
    public int ResetCount { get; set; } = -1;
    [JsonProperty("restartCount")]
    public int RestartCount { get; set; } = -1;
    [JsonProperty("dataExcutionPolicy")]
    public string DataExcutionPolicy { get; set; } = string.Empty;
    [JsonProperty("bitLockerStatus")]
    public string BitLockerStatus { get; set; } = string.Empty;
    [JsonProperty("bootManagerVersion")]
    public string BootManagerVersion { get; set; } = string.Empty;
    [JsonProperty("codeIntegrityCheckVersion")]
    public string CodeIntegrityCheckVersion { get; set; } = string.Empty;
    [JsonProperty("secureBoot")]
    public string SecureBoot { get; set; } = string.Empty;
    [JsonProperty("bootDebugging")]
    public string BootDebugging { get; set; } = string.Empty;
    [JsonProperty("operatingSystemKernelDebugging")]
    public string OperatingSystemKernelDebugging { get; set; } = string.Empty;
    [JsonProperty("codeIntegrity")]
    public string CodeIntegrity { get; set; } = string.Empty;
    [JsonProperty("testSigning")]
    public string TestSigning { get; set; } = string.Empty;
    [JsonProperty("safeMode")]
    public string SafeMode { get; set; } = string.Empty;
    [JsonProperty("windowsPE")]
    public string WindowsPE { get; set; } = string.Empty;
    [JsonProperty("earlyLaunchAntiMalwareDriverProtection")]
    public string EarlyLaunchAntiMalwareDriverProtection { get; set; } = string.Empty;
    [JsonProperty("virtualSecureMode")]
    public string VirtualSecureMode { get; set; } = string.Empty;
    [JsonProperty("pcrHashAlgorithm")]
    public string PcrHashAlgorithm { get; set; } = string.Empty;
    [JsonProperty("bootAppSecurityVersion")]
    public string BootAppSecurityVersion { get; set; } = string.Empty;
    [JsonProperty("bootManagerSecurityVersion")]
    public string BootManagerSecurityVersion { get; set; } = string.Empty;
    [JsonProperty("tpmVersion")]
    public string TpmVersion { get; set; } = string.Empty;
    [JsonProperty("pcr0")]
    public string Pcr0 { get; set; } = string.Empty;
    [JsonProperty("secureBootConfigurationPolicyFingerPrint")]
    public string SecureBootConfigurationPolicyFingerPrint { get; set; } = string.Empty;
    [JsonProperty("codeIntegrityPolicy")]
    public string CodeIntegrityPolicy { get; set; } = string.Empty;
    [JsonProperty("bootRevisionListInfo")]
    public string BootRevisionListInfo { get; set; } = string.Empty;
    [JsonProperty("operatingSystemRevListInfo")]
    public string OperatingSystemRevListInfo { get; set; } = string.Empty;
    [JsonProperty("healthStatusMismatchInfo")]
    public string HealthStatusMismatchInfo { get; set; } = string.Empty;
    [JsonProperty("healthAttestationSupportedStatus")]
    public string HealthAttestationSupportedStatus { get; set; } = string.Empty;
}
