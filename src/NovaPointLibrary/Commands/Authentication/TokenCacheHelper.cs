using Microsoft.Identity.Client.Extensions.Msal;
using NovaPointLibrary.Core.Logging;
using NovaPointLibrary.Core.Settings;

namespace NovaPointLibrary.Commands.Authentication
{
    internal class TokenCacheHelper
    {
        private static readonly int s_version = 1;

        private static readonly string s_cacheFilePath = Path.Combine(AppFolders.GetConfigFolder(),$"msal{s_version}", "msal.cache");

        private static readonly string s_cacheFileName = Path.GetFileName(s_cacheFilePath);
        private static readonly string? s_cacheDir = Path.GetDirectoryName(s_cacheFilePath);


        private static readonly string s_keyChainServiceName = "NovaPoint";
        private static readonly string s_keyChainAccountName = "MSALCache";

        private static readonly string s_linuxKeyRingSchema = "com.github.barbarur.novapoint.tokencache";
        private static readonly string s_linuxKeyRingCollection = MsalCacheHelper.LinuxKeyRingDefaultCollection;
        private static readonly string s_linuxKeyRingLabel = "MSAL token cache for NovaPoint.";
        private static readonly KeyValuePair<string, string> s_linuxKeyRingAttr1 = new KeyValuePair<string, string>("Version", $"{s_version}");
        private static readonly KeyValuePair<string, string> s_linuxKeyRingAttr2 = new KeyValuePair<string, string>("ProductGroup", "NovaPoint");

        internal static async Task<MsalCacheHelper?> GetCache(ILogger? logger = null)
        {
            var storageProperties =

                new StorageCreationPropertiesBuilder(s_cacheFileName, s_cacheDir)
                .WithLinuxKeyring(
                    s_linuxKeyRingSchema,
                    s_linuxKeyRingCollection,
                    s_linuxKeyRingLabel,
                    s_linuxKeyRingAttr1,
                    s_linuxKeyRingAttr2)
                .WithMacKeyChain(
                    s_keyChainServiceName,
                    s_keyChainAccountName)
                .Build();

            var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);

            try
            {
                cacheHelper.VerifyPersistence();
            }
            catch (MsalCachePersistenceException ex)
            {
                logger?.Info(nameof(TokenCacheHelper),
                    $"WARNING: OS secret store unavailable; tokens will NOT be persisted this session. {ex.Message}");
                return null;
            }

            return cacheHelper;
        }

        internal static void RemoveCache()
        {
            File.Delete(s_cacheFilePath);
        }
    }
}
