using static LanguageExt.Prelude;

namespace Dbosoft.OVN.Model.OVN;

public record SouthboundSsl : OVSTableRecord, IHasParentReference, IOVSEntityWithName
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

    public string? PrivateKey => GetValue<string>("private_key");

    public string? Certificate => GetValue<string>("certificate");
    
    public string? CaCertificate => GetValue<string>("ca_cert");

    public string? SslProtocols => GetValue<string>("ssl_protocols");

    public string? SslCiphers => GetValue<string>("ssl_ciphers");

    public string? Name => "SSL";

    public OVSParentReference GetParentReference()
    {
        return new OVSParentReference(
            OVNSouthboundTableNames.Global,
            Optional(GetValue<Guid>("__parentId")).Map(i => i.ToString("D")),
            "ssl");
    }
}
