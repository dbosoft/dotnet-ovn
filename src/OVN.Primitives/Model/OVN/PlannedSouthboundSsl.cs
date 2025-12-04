namespace Dbosoft.OVN.Model.OVN;

public record PlannedSouthboundSsl : PlannedOvsSsl
{
    public new static readonly IDictionary<string, OVSFieldMetadata>
        Columns = new Dictionary<string, OVSFieldMetadata>(PlannedOvsSsl.Columns)
        {
            { "private_key", OVSValue<string>.Metadata() },
            { "certificate", OVSValue<string>.Metadata() },
            { "ca_cert", OVSValue<string>.Metadata() },
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
        return new OVSParentReference(OVNSouthboundTableNames.Global, ".", "ssl");
    }
}
