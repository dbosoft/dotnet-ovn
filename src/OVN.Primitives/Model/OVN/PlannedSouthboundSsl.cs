namespace Dbosoft.OVN.Model.OVN;

public record PlannedSouthboundSsl : OVSEntity, IOVSEntityWithName, IHasParentReference
{
    public new static readonly IDictionary<string, OVSFieldMetadata>
        Columns = new Dictionary<string, OVSFieldMetadata>(OVSTableRecord.Columns)
        {
            { "private_key", OVSValue<string>.Metadata() },
            { "certificate", OVSValue<string>.Metadata() },
            { "ca_cert", OVSValue<string>.Metadata() },
            { "ssl_protocols", OVSValue<string>.Metadata() },
            { "ssl_ciphers", OVSValue<string>.Metadata() },
        };

    public string? PrivateKey
    {
        get => GetValue<string>("private_key");
        init => SetValue("private_key", value);
    }

    public string? Certificate
    {
        get => GetValue<string>("certificate");
        init => SetValue("certificate", value);
    } 

    public string? CaCertificate
    {
        get => GetValue<string>("ca_cert");
        init => SetValue("ca_cert", value);
    }

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

    public string? Name => "SSL";

    public OVSParentReference GetParentReference()
    {
        return new OVSParentReference(OVNSouthboundTableNames.Global, ".", "ssl");
    }
}
