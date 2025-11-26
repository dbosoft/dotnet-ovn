using System.Security.Cryptography;
using System.Text;

namespace Dbosoft.OVN.Core.IntegrationTests;

public static class HashHelper
{
    public static string ComputeSha256(string content)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
