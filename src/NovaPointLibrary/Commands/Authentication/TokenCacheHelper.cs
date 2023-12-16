using Microsoft.Identity.Client.Extensions.Msal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.Authentication
{
    internal class TokenCacheHelper
    {
        private static readonly string s_cacheFilePath = Path.Combine(AppSettings.GetLocalAppPath(), "msal.cache");

        private static readonly string CacheFileName = Path.GetFileName(s_cacheFilePath);
        private static readonly string CacheDir = Path.GetDirectoryName(s_cacheFilePath);


        private static readonly string KeyChainServiceName = "Contoso.MyProduct";
        private static readonly string KeyChainAccountName = "MSALCache";

        private static readonly string LinuxKeyRingSchema = "com.contoso.devtools.tokencache";
        private static readonly string LinuxKeyRingCollection = MsalCacheHelper.LinuxKeyRingDefaultCollection;
        private static readonly string LinuxKeyRingLabel = "MSAL token cache for all Contoso dev tool apps.";
        private static readonly KeyValuePair<string, string> LinuxKeyRingAttr1 = new KeyValuePair<string, string>("Version", "1");
        private static readonly KeyValuePair<string, string> LinuxKeyRingAttr2 = new KeyValuePair<string, string>("ProductGroup", "MyApps");

        internal static async Task<MsalCacheHelper> GetCache()
        {
            var storageProperties =

                new StorageCreationPropertiesBuilder(CacheFileName, CacheDir)
                .WithLinuxKeyring(
                    LinuxKeyRingSchema,
                    LinuxKeyRingCollection,
                    LinuxKeyRingLabel,
                    LinuxKeyRingAttr1,
                    LinuxKeyRingAttr2)
                .WithMacKeyChain(
                    KeyChainServiceName,
                    KeyChainAccountName)
                .Build();

            // This hooks up the cross-platform cache into MSAL
            var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
            return cacheHelper;
        }

        internal static void RemoveCache()
        {
            File.Delete(s_cacheFilePath);
        }
    }
}
