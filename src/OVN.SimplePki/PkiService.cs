using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Dbosoft.OVN.SimplePki;

/// <summary>
/// A simple PKI service for generating OVS/OVN compatible certificates.
/// The CA private key is stored in the file system.
/// </summary>
/// <remarks>
/// This implementation is indented demonstration and testing purposes.
/// Consumers are expected to use their PKI implementation.
/// </remarks>
public class PkiService(ISystemEnvironment systemEnvironment) : IPkiService
{
    private static readonly OvsFile CaCertificate = new("/var/lib/openvswitch/pki/dotnetovnca", "cacert.pem");
    private static readonly OvsFile CaPrivateKey = new("/var/lib/openvswitch/pki/dotnetovnca/private", "cakey.pem");

    public static readonly string ClientAuthenticationOId = "1.3.6.1.5.5.7.3.2";
    public static readonly string ServerAuthenticationOId = "1.3.6.1.5.5.7.3.1";

    private static readonly TimeSpan LifeSpan = TimeSpan.FromDays(10 * 365);

    // The ovs-pki tool generates RSA certificates but current versions
    // of OVS/OVN support ECDSA certificates (via the included OpenSSL).
    // We use the ECDSA certificates as they are significantly smaller.
    private static readonly ECCurve Curve = ECCurve.NamedCurves.nistP256;
    
    public async Task InitializeAsync()
    {
        using var keyPair = ECDsa.Create(Curve);

        var subjectNameBuilder = new X500DistinguishedNameBuilder();
        subjectNameBuilder.AddOrganizationName("dotnet-ovn");
        subjectNameBuilder.AddCommonName("Certificate Authority");
        
        var subjectName = subjectNameBuilder.Build();
        var request = new CertificateRequest(subjectName, keyPair, HashAlgorithmName.SHA256);
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
        await systemEnvironment.FileSystem.WriteFileAsync(
            CaCertificate,
            NormalizePem(certificate.ExportCertificatePem()));

        systemEnvironment.FileSystem.EnsurePathForFileExists(CaPrivateKey, adminOnly: true);
        await systemEnvironment.FileSystem.WriteFileAsync(
            CaPrivateKey,
            NormalizePem(keyPair.ExportPkcs8PrivateKeyPem()));
    }

    public async Task<OvsPkiConfig> GenerateChassisPkiAsync(string chassisName)
    {
        if(!systemEnvironment.FileSystem.FileExists(CaCertificate)
            || !systemEnvironment.FileSystem.FileExists(CaPrivateKey))
            throw new InvalidOperationException("CA certificate or private key not found. Please initialize the PKI.");

        var caCertificatePem = await systemEnvironment.FileSystem.ReadFileAsync(CaCertificate);
        var caPrivateKeyPem = await systemEnvironment.FileSystem.ReadFileAsync(CaPrivateKey);
        using var caPrivateKey = ECDsa.Create();
        caPrivateKey.ImportFromPem(caPrivateKeyPem);
        var caPublicKey = new PublicKey(caPrivateKey);
        using var caCertificate = X509Certificate2.CreateFromPem(caCertificatePem);
        using var caCertificateWithKey = caCertificate.CopyWithPrivateKey(caPrivateKey);

        using var keyPair = ECDsa.Create(Curve);
        var publicKey = new PublicKey(keyPair);

        var subjectNameBuilder = new X500DistinguishedNameBuilder();
        subjectNameBuilder.AddOrganizationName("dotnet-ovn");
        subjectNameBuilder.AddCommonName("Certificate Authority");
        var subjectName = subjectNameBuilder.Build();

        var subjectAlternativeNameBuilder = new SubjectAlternativeNameBuilder();
        subjectAlternativeNameBuilder.AddDnsName(chassisName);
        var subjectAlternativeName = subjectAlternativeNameBuilder.Build();

        var request = new CertificateRequest(subjectName, keyPair, HashAlgorithmName.SHA256)
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

        return new OvsPkiConfig(
            NormalizePem(keyPair.ExportPkcs8PrivateKeyPem()),
            NormalizePem(certificate.ExportCertificatePem()),
            NormalizePem(caCertificatePem));
    }

    private static string NormalizePem(string pem)
    {
        return pem.ReplaceLineEndings("\n").Trim() + "\n";
    }
}
