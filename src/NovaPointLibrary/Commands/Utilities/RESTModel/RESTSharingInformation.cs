using Microsoft.Graph;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.Utilities.RESTModel
{
    internal class RESTSharingInformation
    {
        [JsonProperty("odata.metadata")]
        public string odatametadata { get; set; }

        [JsonProperty("odata.type")]
        public string odatatype { get; set; }

        [JsonProperty("odata.id")]
        public string odataid { get; set; }

        [JsonProperty("odata.editLink")]
        public string odataeditLink { get; set; }

        [JsonProperty("pickerSettings@odata.navigationLinkUrl")]
        public string pickerSettingsodatanavigationLinkUrl { get; set; }
        public PickerSettings pickerSettings { get; set; }
        public int ageGroup { get; set; }
        public string allOrganizationSecurityGroupId { get; set; }
        public int anonymousLinkExpirationRestrictionDays { get; set; }
        public bool anyoneLinkTrackUsers { get; set; }
        public bool blockPeoplePickerAndSharing { get; set; }
        public bool canAddExistingExternalPrincipal { get; set; }
        public bool canAddExternalPrincipal { get; set; }
        public bool canAddInternalPrincipal { get; set; }
        public bool canRequestAccessForGrantAccess { get; set; }
        public bool canSendEmail { get; set; }
        public bool canUseSimplifiedRoles { get; set; }
        public int currentRole { get; set; }
        public string customizedExternalSharingServiceUrl { get; set; }
        public int defaultLinkKind { get; set; }
        public int defaultShareLinkPermission { get; set; }
        public int defaultShareLinkScope { get; set; }
        public bool defaultShareLinkToExistingAccess { get; set; }
        public string directUrl { get; set; }
        public bool discoverableByOrganizationEnabled { get; set; }
        public string displayName { get; set; }
        public bool enforceIBSegmentFiltering { get; set; }
        public bool enforceSPOSearch { get; set; }
        public string fileExtension { get; set; }
        public bool hasUniquePermissions { get; set; }
        public bool isConsumerFiles { get; set; }
        public bool isPremium { get; set; }
        public bool isStubFile { get; set; }
        public string itemUniqueId { get; set; }
        public string itemUrl { get; set; }
        public string microserviceShareUiUrl { get; set; }
        public string outlookEndpointHostUrl { get; set; }
        public PermissionsInformation permissionsInformation { get; set; }
        public RecipientLimits recipientLimits { get; set; }
        public object sensitivityLabelInformation { get; set; }
        public int sharedObjectType { get; set; }
        public string shareUiUrl { get; set; }
        public SharingAbilities sharingAbilities { get; set; }
        public int sharingStatus { get; set; }
        public bool showExternalSharingWarning { get; set; }
        public string siteIBMode { get; set; }
        public List<object> siteIBSegmentIDs { get; set; }
        public string siteId { get; set; }
        public bool standardRolesModified { get; set; }
        public string substrateFileId { get; set; }
        public string tenantDisplayName { get; set; }
        public string tenantId { get; set; }
        public object userIsSharingViaMCS { get; set; }
        public string userPhotoCdnBaseUrl { get; set; }
        public int webTemplateId { get; set; }
        public string webUrl { get; set; }
    }

    public class AnonymousLinkAbilities
    {
        public CanAddNewExternalPrincipals canAddNewExternalPrincipals { get; set; }
        public CanDeleteEditLink canDeleteEditLink { get; set; }
        public CanDeleteManageListLink canDeleteManageListLink { get; set; }
        public CanDeleteReadLink canDeleteReadLink { get; set; }
        public CanDeleteReviewLink canDeleteReviewLink { get; set; }
        public CanDeleteSubmitOnlyLink canDeleteSubmitOnlyLink { get; set; }
        public CanGetEditLink canGetEditLink { get; set; }
        public CanGetManageListLink canGetManageListLink { get; set; }
        public CanGetReadLink canGetReadLink { get; set; }
        public CanGetReviewLink canGetReviewLink { get; set; }
        public CanGetSubmitOnlyLink canGetSubmitOnlyLink { get; set; }
        public CanHaveExternalUsers canHaveExternalUsers { get; set; }
        public CanManageEditLink canManageEditLink { get; set; }
        public CanManageManageListLink canManageManageListLink { get; set; }
        public CanManageReadLink canManageReadLink { get; set; }
        public CanManageReviewLink canManageReviewLink { get; set; }
        public CanManageSubmitOnlyLink canManageSubmitOnlyLink { get; set; }
        public LinkExpiration linkExpiration { get; set; }
        public PasswordProtected passwordProtected { get; set; }
        public SubmitOnlylinkExpiration submitOnlylinkExpiration { get; set; }
        public SupportsRestrictedView supportsRestrictedView { get; set; }
        public SupportsRestrictToExistingRelationships supportsRestrictToExistingRelationships { get; set; }
        public object trackLinkUsers { get; set; }
    }

    public class AnyoneLinkAbilities
    {
        public CanAddNewExternalPrincipals canAddNewExternalPrincipals { get; set; }
        public CanDeleteEditLink canDeleteEditLink { get; set; }
        public CanDeleteManageListLink canDeleteManageListLink { get; set; }
        public CanDeleteReadLink canDeleteReadLink { get; set; }
        public CanDeleteReviewLink canDeleteReviewLink { get; set; }
        public CanDeleteSubmitOnlyLink canDeleteSubmitOnlyLink { get; set; }
        public CanGetEditLink canGetEditLink { get; set; }
        public CanGetManageListLink canGetManageListLink { get; set; }
        public CanGetReadLink canGetReadLink { get; set; }
        public CanGetReviewLink canGetReviewLink { get; set; }
        public CanGetSubmitOnlyLink canGetSubmitOnlyLink { get; set; }
        public CanHaveExternalUsers canHaveExternalUsers { get; set; }
        public CanManageEditLink canManageEditLink { get; set; }
        public CanManageManageListLink canManageManageListLink { get; set; }
        public CanManageReadLink canManageReadLink { get; set; }
        public CanManageReviewLink canManageReviewLink { get; set; }
        public CanManageSubmitOnlyLink canManageSubmitOnlyLink { get; set; }
        public LinkExpiration linkExpiration { get; set; }
        public PasswordProtected passwordProtected { get; set; }
        public SubmitOnlylinkExpiration submitOnlylinkExpiration { get; set; }
        public SupportsRestrictedView supportsRestrictedView { get; set; }
        public SupportsRestrictToExistingRelationships supportsRestrictToExistingRelationships { get; set; }
        public object trackLinkUsers { get; set; }
    }

    public class CanAddExternalPrincipal
    {
        public int disabledReason { get; set; }
        public bool enabled { get; set; }
    }

    public class CanAddInternalPrincipal
    {
        public int disabledReason { get; set; }
        public bool enabled { get; set; }
    }

    public class CanAddNewExternalPrincipal
    {
        public int disabledReason { get; set; }
        public bool enabled { get; set; }
    }

    public class CanAddNewExternalPrincipals
    {
        public int disabledReason { get; set; }
        public bool enabled { get; set; }
    }

    public class CanDeleteEditLink
    {
        public int disabledReason { get; set; }
        public bool enabled { get; set; }
    }

    public class CanDeleteManageListLink
    {
        public int disabledReason { get; set; }
        public bool enabled { get; set; }
    }

    public class CanDeleteReadLink
    {
        public int disabledReason { get; set; }
        public bool enabled { get; set; }
    }

    public class CanDeleteReviewLink
    {
        public int disabledReason { get; set; }
        public bool enabled { get; set; }
    }

    public class CanDeleteSubmitOnlyLink
    {
        public int disabledReason { get; set; }
        public bool enabled { get; set; }
    }

    public class CanGetEditLink
    {
        public int disabledReason { get; set; }
        public bool enabled { get; set; }
    }

    public class CanGetManageListLink
    {
        public int disabledReason { get; set; }
        public bool enabled { get; set; }
    }

    public class CanGetReadLink
    {
        public int disabledReason { get; set; }
        public bool enabled { get; set; }
    }

    public class CanGetReviewLink
    {
        public int disabledReason { get; set; }
        public bool enabled { get; set; }
    }

    public class CanGetSubmitOnlyLink
    {
        public int disabledReason { get; set; }
        public bool enabled { get; set; }
    }

    public class CanHaveExternalUsers
    {
        public int disabledReason { get; set; }
        public bool enabled { get; set; }
    }

    public class CanManageEditLink
    {
        public int disabledReason { get; set; }
        public bool enabled { get; set; }
    }

    public class CanManageManageListLink
    {
        public int disabledReason { get; set; }
        public bool enabled { get; set; }
    }

    public class CanManageReadLink
    {
        public int disabledReason { get; set; }
        public bool enabled { get; set; }
    }

    public class CanManageReviewLink
    {
        public int disabledReason { get; set; }
        public bool enabled { get; set; }
    }

    public class CanManageSubmitOnlyLink
    {
        public int disabledReason { get; set; }
        public bool enabled { get; set; }
    }

    public class CanRequestGrantAccess
    {
        public int disabledReason { get; set; }
        public bool enabled { get; set; }
    }

    public class CheckPermissions
    {
        public int AliasOnly { get; set; }
        public int EmailOnly { get; set; }
        public int MixedRecipients { get; set; }
        public int ObjectIdOnly { get; set; }
    }

    public class CreatedBy
    {
        public object directoryObjectId { get; set; }
        public string email { get; set; }
        public string expiration { get; set; }
        public int id { get; set; }
        public bool isActive { get; set; }
        public bool isExternal { get; set; }
        public object jobTitle { get; set; }
        public string loginName { get; set; }
        public string name { get; set; }
        public int principalType { get; set; }
        public object userId { get; set; }
        public string userPrincipalName { get; set; }
    }

    public class DirectSharingAbilities
    {
        public CanAddExternalPrincipal canAddExternalPrincipal { get; set; }
        public CanAddInternalPrincipal canAddInternalPrincipal { get; set; }
        public CanAddNewExternalPrincipal canAddNewExternalPrincipal { get; set; }
        public CanRequestGrantAccess canRequestGrantAccess { get; set; }
        public SupportsEditPermission supportsEditPermission { get; set; }
        public SupportsManageListPermission supportsManageListPermission { get; set; }
        public SupportsReadPermission supportsReadPermission { get; set; }
        public SupportsRestrictedViewPermission supportsRestrictedViewPermission { get; set; }
        public SupportsReviewPermission supportsReviewPermission { get; set; }
    }

    public class GrantDirectAccess
    {
        public int AliasOnly { get; set; }
        public int EmailOnly { get; set; }
        public int MixedRecipients { get; set; }
        public int ObjectIdOnly { get; set; }
    }

    public class LastModifiedBy
    {
        public object directoryObjectId { get; set; }
        public string email { get; set; }
        public string expiration { get; set; }
        public int id { get; set; }
        public bool isActive { get; set; }
        public bool isExternal { get; set; }
        public object jobTitle { get; set; }
        public string loginName { get; set; }
        public string name { get; set; }
        public int principalType { get; set; }
        public object userId { get; set; }
        public string userPrincipalName { get; set; }
    }

    public class Link
    {
        public object inheritedFrom { get; set; }
        public bool isInherited { get; set; }
        public LinkDetails linkDetails { get; set; }
        public List<object> linkMembers { get; set; }
        public LinkStatus linkStatus { get; set; }
        public int totalLinkMembersCount { get; set; }
    }

    public class LinkDetails
    {
        public bool AllowsAnonymousAccess { get; set; }
        public object ApplicationId { get; set; }
        public bool BlocksDownload { get; set; }
        public string Created { get; set; }
        public CreatedBy CreatedBy { get; set; }
        public object Description { get; set; }
        public bool Embeddable { get; set; }
        public string Expiration { get; set; }
        public bool HasExternalGuestInvitees { get; set; }
        public List<Invitation> Invitations { get; set; }
        public bool IsActive { get; set; }
        public bool IsAddressBarLink { get; set; }
        public bool IsCreateOnlyLink { get; set; }
        public bool IsDefault { get; set; }
        public bool IsEditLink { get; set; }
        public bool IsEphemeral { get; set; }
        public bool IsFormsLink { get; set; }
        public bool IsManageListLink { get; set; }
        public bool IsReviewLink { get; set; }
        public bool IsUnhealthy { get; set; }
        public string LastModified { get; set; }
        public LastModifiedBy LastModifiedBy { get; set; }
        public bool LimitUseToApplication { get; set; }
        public int LinkKind { get; set; }
        public object MeetingId { get; set; }
        public string PasswordLastModified { get; set; }
        public object PasswordLastModifiedBy { get; set; }
        public List<object> RedeemedUsers { get; set; }
        public bool RequiresPassword { get; set; }
        public bool RestrictedShareMembership { get; set; }
        public bool RestrictToExistingRelationships { get; set; }
        public int Scope { get; set; }
        public string ShareId { get; set; }
        public string ShareTokenString { get; set; }
        public int SharingLinkStatus { get; set; }
        public bool TrackLinkUsers { get; set; }
        public string Url { get; set; }
    }

    public class LinkExpiration
    {
        public int disabledReason { get; set; }
        public bool enabled { get; set; }
        public int defaultExpirationInDays { get; set; }
    }

    public class LinkStatus
    {
        public int disabledReason { get; set; }
        public bool enabled { get; set; }
    }

    public class OrganizationLinkAbilities
    {
        public CanAddNewExternalPrincipals canAddNewExternalPrincipals { get; set; }
        public CanDeleteEditLink canDeleteEditLink { get; set; }
        public CanDeleteManageListLink canDeleteManageListLink { get; set; }
        public CanDeleteReadLink canDeleteReadLink { get; set; }
        public CanDeleteReviewLink canDeleteReviewLink { get; set; }
        public object canDeleteSubmitOnlyLink { get; set; }
        public CanGetEditLink canGetEditLink { get; set; }
        public CanGetManageListLink canGetManageListLink { get; set; }
        public CanGetReadLink canGetReadLink { get; set; }
        public CanGetReviewLink canGetReviewLink { get; set; }
        public object canGetSubmitOnlyLink { get; set; }
        public CanHaveExternalUsers canHaveExternalUsers { get; set; }
        public CanManageEditLink canManageEditLink { get; set; }
        public CanManageManageListLink canManageManageListLink { get; set; }
        public CanManageReadLink canManageReadLink { get; set; }
        public CanManageReviewLink canManageReviewLink { get; set; }
        public object canManageSubmitOnlyLink { get; set; }
        public LinkExpiration linkExpiration { get; set; }
        public PasswordProtected passwordProtected { get; set; }
        public object submitOnlylinkExpiration { get; set; }
        public SupportsRestrictedView supportsRestrictedView { get; set; }
        public object supportsRestrictToExistingRelationships { get; set; }
        public object trackLinkUsers { get; set; }
    }

    public class PasswordProtected
    {
        public int disabledReason { get; set; }
        public bool enabled { get; set; }
    }

    public class PeopleSharingLinkAbilities
    {
        public CanAddNewExternalPrincipals canAddNewExternalPrincipals { get; set; }
        public CanDeleteEditLink canDeleteEditLink { get; set; }
        public CanDeleteManageListLink canDeleteManageListLink { get; set; }
        public CanDeleteReadLink canDeleteReadLink { get; set; }
        public CanDeleteReviewLink canDeleteReviewLink { get; set; }
        public object canDeleteSubmitOnlyLink { get; set; }
        public CanGetEditLink canGetEditLink { get; set; }
        public CanGetManageListLink canGetManageListLink { get; set; }
        public CanGetReadLink canGetReadLink { get; set; }
        public CanGetReviewLink canGetReviewLink { get; set; }
        public object canGetSubmitOnlyLink { get; set; }
        public CanHaveExternalUsers canHaveExternalUsers { get; set; }
        public CanManageEditLink canManageEditLink { get; set; }
        public CanManageManageListLink canManageManageListLink { get; set; }
        public CanManageReadLink canManageReadLink { get; set; }
        public CanManageReviewLink canManageReviewLink { get; set; }
        public object canManageSubmitOnlyLink { get; set; }
        public LinkExpiration linkExpiration { get; set; }
        public PasswordProtected passwordProtected { get; set; }
        public object submitOnlylinkExpiration { get; set; }
        public SupportsRestrictedView supportsRestrictedView { get; set; }
        public object supportsRestrictToExistingRelationships { get; set; }
        public object trackLinkUsers { get; set; }
    }

    public class PermissionsInformation
    {
        public List<object> appConsentPrincipals { get; set; }
        public bool hasInheritedLinks { get; set; }
        public List<Link> links { get; set; }
        public List<Principal> principals { get; set; }
        public List<SiteAdmin> siteAdmins { get; set; }
        public int totalNumberOfPrincipals { get; set; }
    }

    public class PickerSettings
    {
        [JsonProperty("odata.type")]
        public string odatatype { get; set; }

        [JsonProperty("odata.id")]
        public string odataid { get; set; }

        [JsonProperty("odata.editLink")]
        public string odataeditLink { get; set; }
        public bool AllowEmailAddresses { get; set; }
        public bool AllowOnlyEmailAddresses { get; set; }
        public string PrincipalAccountType { get; set; }
        public int PrincipalSource { get; set; }
        public QuerySettings QuerySettings { get; set; }
        public bool UseSubstrateSearch { get; set; }
        public int VisibleSuggestions { get; set; }
    }

    public class Principal
    {
        public object ExpirationDateTimeOnACE { get; set; }
        public object inheritedFrom { get; set; }
        public bool isInherited { get; set; }
        public List<object> members { get; set; }
        public Principal principal { get; set; }
        public int role { get; set; }
    }

    public class Principal2
    {
        public object directoryObjectId { get; set; }
        public object email { get; set; }
        public object expiration { get; set; }
        public int id { get; set; }
        public bool isActive { get; set; }
        public bool isExternal { get; set; }
        public object jobTitle { get; set; }
        public string loginName { get; set; }
        public string name { get; set; }
        public int principalType { get; set; }
        public object userId { get; set; }
        public object userPrincipalName { get; set; }
    }

    public class QuerySettings
    {
        public bool ExcludeAllUsersOnTenantClaim { get; set; }
        public bool IsSharing { get; set; }
    }

    public class RecipientLimits
    {
        public CheckPermissions checkPermissions { get; set; }
        public GrantDirectAccess grantDirectAccess { get; set; }
        public ShareLink shareLink { get; set; }
        public ShareLinkWithDeferRedeem shareLinkWithDeferRedeem { get; set; }
    }

    

    public class ShareLink
    {
        public int AliasOnly { get; set; }
        public int EmailOnly { get; set; }
        public int MixedRecipients { get; set; }
        public int ObjectIdOnly { get; set; }
    }

    public class ShareLinkWithDeferRedeem
    {
        public int AliasOnly { get; set; }
        public int EmailOnly { get; set; }
        public int MixedRecipients { get; set; }
        public int ObjectIdOnly { get; set; }
    }

    public class SharingAbilities
    {
        public AnonymousLinkAbilities anonymousLinkAbilities { get; set; }
        public AnyoneLinkAbilities anyoneLinkAbilities { get; set; }
        public bool canStopSharing { get; set; }
        public DirectSharingAbilities directSharingAbilities { get; set; }
        public OrganizationLinkAbilities organizationLinkAbilities { get; set; }
        public PeopleSharingLinkAbilities peopleSharingLinkAbilities { get; set; }
    }

    public class SiteAdmin
    {
        public object ExpirationDateTimeOnACE { get; set; }
        public object inheritedFrom { get; set; }
        public bool isInherited { get; set; }
        public List<object> members { get; set; }
        public Principal principal { get; set; }
        public int role { get; set; }
    }

    public class SubmitOnlylinkExpiration
    {
        public int disabledReason { get; set; }
        public bool enabled { get; set; }
        public int defaultExpirationInDays { get; set; }
    }

    public class SupportsEditPermission
    {
        public int disabledReason { get; set; }
        public bool enabled { get; set; }
    }

    public class SupportsManageListPermission
    {
        public int disabledReason { get; set; }
        public bool enabled { get; set; }
    }

    public class SupportsReadPermission
    {
        public int disabledReason { get; set; }
        public bool enabled { get; set; }
    }

    public class SupportsRestrictedView
    {
        public int disabledReason { get; set; }
        public bool enabled { get; set; }
    }

    public class SupportsRestrictedViewPermission
    {
        public int disabledReason { get; set; }
        public bool enabled { get; set; }
    }

    public class SupportsRestrictToExistingRelationships
    {
        public int disabledReason { get; set; }
        public bool enabled { get; set; }
    }

    public class SupportsReviewPermission
    {
        public int disabledReason { get; set; }
        public bool enabled { get; set; }
    }

    public class Invitation
    {
        public InvitedBy invitedBy { get; set; }
        public DateTime invitedOn { get; set; }
        public Invitee invitee { get; set; }
    }

    public class InvitedBy
    {
        public object directoryObjectId { get; set; }
        public string email { get; set; }
        public string expiration { get; set; }
        public int id { get; set; }
        public bool isActive { get; set; }
        public bool isExternal { get; set; }
        public object jobTitle { get; set; }
        public string loginName { get; set; }
        public string name { get; set; }
        public int principalType { get; set; }
        public object userId { get; set; }
        public string userPrincipalName { get; set; }
    }

    public class Invitee
    {
        public object directoryObjectId { get; set; }
        public string email { get; set; }
        public object expiration { get; set; }
        public int id { get; set; }
        public bool isActive { get; set; }
        public bool isExternal { get; set; }
        public object jobTitle { get; set; }
        public object loginName { get; set; }
        public string name { get; set; }
        public int principalType { get; set; }
        public object userId { get; set; }
        public object userPrincipalName { get; set; }
    }

}
