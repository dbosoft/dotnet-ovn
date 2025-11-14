using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Dbosoft.OVN.SimplePki;

public class PkiService(ISystemEnvironment systemEnvironment) : IPkiService
{
    private static readonly OvsFile CaCertificate = new("/var/lib/openvswitch/pki/dotnetovnca", "cacert.pem");
    private static readonly OvsFile CaPrivateKey = new("/var/lib/openvswitch/pki/dotnetovnca/private", "cakey.pem");

    public static readonly string ClientAuthenticationOId = "1.3.6.1.5.5.7.3.2";
    public static readonly string ServerAuthenticationOId = "1.3.6.1.5.5.7.3.1";

    private static readonly TimeSpan LifeSpan = TimeSpan.FromDays(10 * 365);
    private const int KeySize = 2048;


    public async Task InitializeAsync()
    {
        using var keyPair = RSA.Create(KeySize);

        var subjectNameBuilder = new X500DistinguishedNameBuilder();
        subjectNameBuilder.AddOrganizationName("dotnet-ovn");
        subjectNameBuilder.AddCommonName("Certificate Authority");
        
        var subjectName = subjectNameBuilder.Build();
        var request = new CertificateRequest(subjectName, keyPair, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));

        var publicKey = new PublicKey(keyPair);
        var subjectKeyIdentifier = new X509SubjectKeyIdentifierExtension(publicKey, false);
        var authorityKeyIdentifier = X509AuthorityKeyIdentifierExtension
            .CreateFromSubjectKeyIdentifier(subjectKeyIdentifier);
        request.CertificateExtensions.Add(subjectKeyIdentifier);
        request.CertificateExtensions.Add(authorityKeyIdentifier);

        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign, true));

        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1)),
            DateTimeOffset.UtcNow.Add(LifeSpan));

        systemEnvironment.FileSystem.EnsurePathForFileExists(CaCertificate);
        await systemEnvironment.FileSystem.WriteFileAsync(CaCertificate, certificate.ExportCertificatePem());

        systemEnvironment.FileSystem.EnsurePathForFileExists(CaPrivateKey);
        await systemEnvironment.FileSystem.WriteFileAsync(CaPrivateKey, keyPair.ExportRSAPrivateKeyPem());
    }

    public async Task<ChassisPkiResult> GenerateChassisPkiAsync(string chassisName)
    {
        if(!systemEnvironment.FileSystem.FileExists(CaCertificate)
            || !systemEnvironment.FileSystem.FileExists(CaPrivateKey))
            throw new InvalidOperationException("CA certificate or private key not found. Please initialize the PKI.");

        var caCertificatePem = await systemEnvironment.FileSystem.ReadFileAsync(CaCertificate);
        var caPrivateKeyPem = await systemEnvironment.FileSystem.ReadFileAsync(CaPrivateKey);
        using var caPrivateKey = RSA.Create();
        caPrivateKey.ImportFromPem(caPrivateKeyPem);
        var caPublicKey = new PublicKey(caPrivateKey);
        using var caCertificate = X509Certificate2.CreateFromPem(caCertificatePem);
        using var caCertificateWithKey = caCertificate.CopyWithPrivateKey(caPrivateKey);

        using var keyPair = RSA.Create(KeySize);
        var publicKey = new PublicKey(keyPair);

        var subjectNameBuilder = new X500DistinguishedNameBuilder();
        subjectNameBuilder.AddOrganizationName("dotnet-ovn");
        subjectNameBuilder.AddCommonName("Certificate Authority");
        var subjectName = subjectNameBuilder.Build();

        var subjectAlternativeNameBuilder = new SubjectAlternativeNameBuilder();
        subjectAlternativeNameBuilder.AddDnsName(chassisName);
        var subjectAlternativeName = subjectAlternativeNameBuilder.Build();

        var request = new CertificateRequest(subjectName, keyPair, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
        {
            CertificateExtensions =
            {
                new X509BasicConstraintsExtension(false, false, 0, true),
                subjectAlternativeName,
                new X509SubjectKeyIdentifierExtension(publicKey, false),
                X509AuthorityKeyIdentifierExtension.CreateFromSubjectKeyIdentifier(
                    new X509SubjectKeyIdentifierExtension(caPublicKey, false)),
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                    true),
                new X509EnhancedKeyUsageExtension(
                    [
                        Oid.FromOidValue(ClientAuthenticationOId, OidGroup.EnhancedKeyUsage),
                        Oid.FromOidValue(ServerAuthenticationOId, OidGroup.EnhancedKeyUsage),
                    ],
                    true),
            }
        };
        
        var serial = new byte[16];
        RandomNumberGenerator.Fill(serial);

        using var certificate = request.Create(
            caCertificateWithKey,
            DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1)),
            caCertificateWithKey.NotAfter,
            serial);

        return new ChassisPkiResult(
            keyPair.ExportRSAPrivateKeyPem(),
            certificate.ExportCertificatePem(),
            caCertificatePem);
    }
}
