using System.Security.Cryptography.X509Certificates;


namespace NovaPointLibrary.Core.Authentication
{
    public class AppClientConfidentialProperties : IAppClientProperties
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ClientTitle { get; set; } = "New App-Only";
        public Guid TenantId { get; set; } = Guid.Empty;
        public Guid ClientId { get; set; } = Guid.Empty;
        public string CertificatePath { get; set; } = string.Empty;
        public string Password {  get; set; } = string.Empty;
        internal X509Certificate2 Certificate
        {
            get
            {
                return new X509Certificate2(CertificatePath, Password);
            }
        }

        public AppClientConfidentialProperties() { }
        public AppClientConfidentialProperties(Guid tenantId, Guid clientId, string certificatePath)
        {
            TenantId = tenantId;
            ClientId = clientId;
            CertificatePath = certificatePath;
        }

        internal void ValidateProperties()
        {
            if (TenantId == Guid.Empty)
            {
                throw new Exception("Incorrect Tenant ID");
            }
            if (ClientId == Guid.Empty)
            {
                throw new Exception("Incorrect Client ID");
            }
            if (string.IsNullOrWhiteSpace(CertificatePath))
            {
                throw new Exception("Missing certificate path");
            }
            if (!File.Exists(CertificatePath))
            {
                throw new Exception("Certificate no found on path");
            }
            if (string.IsNullOrWhiteSpace(Password))
            {
                throw new Exception("Certificate password is empty");
            }
        }
    }
}
