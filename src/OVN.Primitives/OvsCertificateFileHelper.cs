using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using LanguageExt;
using LanguageExt.Common;

namespace Dbosoft.OVN;

public static class OvsCertificateFileHelper
{
    public static OvsFile ComputeCaCertificatePath(string caCertificate) =>
        new("/etc/openvswitch", $"cacert_{ComputeHash(caCertificate)}.pem");

    public static OvsFile ComputeCertificatePath(string certificate) =>
        new("/etc/openvswitch", $"cert_{ComputeHash(certificate)}.pem");

    public static OvsFile ComputePrivateKeyPath(string privateKey) =>
        new("/etc/openvswitch", $"privkey_{ComputeHash(privateKey)}.pem");

    private static string ComputeHash(string content)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
