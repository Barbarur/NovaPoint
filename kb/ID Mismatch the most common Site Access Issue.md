# Symptoms

A user has been granted permission to a site or a folder of a site. But when trying to access the site the user get an error message ***“Denied Access”*** or ***“This link is not for you”***.

In the majority of the cases this is due an ID Mismatch issue of the user in that particular site.


# Root cause

Each user, both internal and external, get an **Object ID** on **Azure AD** when they are created. Then the account syncs with SharePoint and it gets its own **SharePoint ID** based on the Azure AD.

When the account is deleted then a new account is created with the same *UPN* as the previous one, or it was deactivated and later recreated, the account will get a new Object ID in Azure AD. Due this change in Azure AD, SharePoint Online also grants a new SharePoint ID for this account.

Meanwhile, each SharePoint Site keeps a record of each account that has ever accessed the site, even if those accounts don’t have permissions to the site anymore or the user account has been deleted from Azure AD. This users list on each store is not purge automatically.

The problem comes when the newly *recreated/reactivated* account tries to access a Site where the previous account with same UPN accessed before. Because the user list on the Site has the record with the old account with the old ID, the ID Mismatch issue occurs.

You can confirm this ID Mismatch issue running the diagnosis [Check User Access](https://aka.ms/PillarCheckUserAccess) as *Global Admin* or *SharePoint Admin* in MS365 Admin Center.


# Solutions

### Option #1: Use the ID Mismatch QuickFix solution from NovaPoint

[**NovaPoint**](https://github.com/Barbarur/NovaPoint) solutions contains a QuickFix for resolving this [**ID Mismatch**](https://github.com/Barbarur/NovaPoint/wiki/Solution-QuickFix-ID-Match) issue.

This solution can be run for a single Site or across all Sites in the environment to prevent encountering this issue again, which is expected when many files were shared with the old user account.

It also offers a option to run the solution in **Report Mode**. On this mode you will get a report of all the sites where the user is registered with the incorrect ID, but no change will be made on the Site. 

For more information on how the solution works, you can visit the [**ID Mismatch documentation**](https://github.com/Barbarur/NovaPoint/wiki/Solution-QuickFix-ID-Match).

[![QuickFix ID Mismatch](https://img.youtube.com/vi/nk_8i34vdhU/hqdefault.jpg)](https://youtu.be/nk_8i34vdhU)


### Option #2: Removing the user from the Site List

1. Navigate to the Site location */_layouts/15/people.aspx?MembershipGroupId=0* to access the Site user list (i.e. *"https://\<Domain\>.sharepoint.com/sites/\<SiteName\>/_layouts/15/people.aspx?MembershipGroupId=0"*)
2. Look for the affected users on the users list and click on the checkbox at the left of the user name.
3. At the top of the list click on *Actions > Delete user from Site Collection*.
4. Share again the Site/Folder with the affected user.

### Option #3: Using SharePoint Self Diagnosis

Navigate to MS365 Admin Center as *Global Admin* or *SharePoint Admin* and run the diagnosis [Site User ID Mismatch](https://aka.ms/PillarSiteUserIDMismatch).

It perform the same actions as the previous option, but automatically. It also resolves the issue only on a single Site per run and got some performance issues before, though Microsoft has implemented some improvements recently.

After running the diagnosis you can share again the Site/Folder with the affected user.

[Fix site user ID mismatch in SharePoint or OneDrive](https://learn.microsoft.com/en-us/sharepoint/troubleshoot/sharing-and-permissions/fix-site-user-id-mismatch)

After running the diagnosis you can share again the Site/Folder with the affected user.
