namespace Dbosoft.OVN.Model.OVN;

public record PlannedNorthboundSsl : PlannedOvsSsl
{
    public new static readonly IDictionary<string, OVSFieldMetadata>
        Columns = new Dictionary<string, OVSFieldMetadata>(PlannedOvsSsl.Columns)
        {
            { "ssl_protocols", OVSValue<string>.Metadata() },
            { "ssl_ciphers", OVSValue<string>.Metadata() },
            { "ssl_ciphersuites", OVSValue<string>.Metadata() },
        };

    public string? SslProtocols
    {
        get => GetValue<string>("ssl_protocols");
        init => SetValue("ssl_protocols", value);
    }

    public string? SslCiphers
    {
        get => GetValue<string>("ssl_ciphers");
        init => SetValue("ssl_ciphers", value);
    }

    public string? SslCipherSuites
    {
        get => GetValue<string>("ssl_ciphersuites");
        init => SetValue("ssl_ciphersuites", value);
    }

    public override OVSParentReference GetParentReference()
    {
        return new OVSParentReference(OVNTableNames.Global, ".", "ssl");
    }
}
