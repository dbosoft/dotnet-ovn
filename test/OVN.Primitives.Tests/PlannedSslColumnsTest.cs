using Dbosoft.OVN.Model.OVN;

namespace Dbosoft.OVN.Primitives.Tests;

public class PlannedSslColumnsTest
{
    // The planned SSL Columns extend the base PlannedOvsSsl metadata (which
    // already declares private_key/certificate/ca_cert) with the OVN-specific
    // ssl_* columns. Accessing the static field must not throw (a duplicate key
    // in the initializer would fail static initialization) and must expose both
    // the base and the ssl_* columns.

    [Fact]
    public void PlannedNorthboundSsl_Columns_contain_base_and_ssl_columns()
    {
        var columns = PlannedNorthboundSsl.Columns;

        Assert.Contains("private_key", columns.Keys);
        Assert.Contains("certificate", columns.Keys);
        Assert.Contains("ca_cert", columns.Keys);
        Assert.Contains("ssl_protocols", columns.Keys);
        Assert.Contains("ssl_ciphers", columns.Keys);
        Assert.Contains("ssl_ciphersuites", columns.Keys);
    }

    [Fact]
    public void PlannedSouthboundSsl_Columns_contain_base_and_ssl_columns()
    {
        var columns = PlannedSouthboundSsl.Columns;

        Assert.Contains("private_key", columns.Keys);
        Assert.Contains("certificate", columns.Keys);
        Assert.Contains("ca_cert", columns.Keys);
        Assert.Contains("ssl_protocols", columns.Keys);
        Assert.Contains("ssl_ciphers", columns.Keys);
        Assert.Contains("ssl_ciphersuites", columns.Keys);
    }
}
