

namespace NovaPointLibrary.Core.Authentication
{
    public class AppClientPublicProperties : IAppClientProperties
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ClientTitle { get; set; } = "Name of delegated app";
        public Guid TenantId { get; set; } = Guid.Empty;
        public Guid ClientId { get; set; } = Guid.Empty;
        public bool CachingToken { get; set; } = false;

        public AppClientPublicProperties() { }

        public AppClientPublicProperties(Guid tenantId, Guid clientId, bool cachingToken)
        {
            TenantId = tenantId;
            ClientId = clientId;
            CachingToken = cachingToken;
        }

        public void ValidateProperties()
        {
            if (TenantId == Guid.Empty || !Guid.TryParse(TenantId.ToString(), out _))
            {
                throw new Exception("Incorrect Tenant ID");
            }
            if (ClientId == Guid.Empty)
            {
                throw new Exception("Incorrect Client ID");
            }
        }

    }
}
