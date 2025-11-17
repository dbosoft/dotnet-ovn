using static LanguageExt.Prelude;

namespace Dbosoft.OVN.Model.OVN;

public record SouthboundSsl : OVSSslTableRecord
{
    public new static readonly IDictionary<string, OVSFieldMetadata>
        Columns = new Dictionary<string, OVSFieldMetadata>(OVSSslTableRecord.Columns)
        {
            { "ssl_protocols", OVSValue<string>.Metadata() },
            { "ssl_ciphers", OVSValue<string>.Metadata() },
            { "ssl_ciphersuites", OVSValue<string>.Metadata() },
        };

    public string? SslProtocols => GetValue<string>("ssl_protocols");

    public string? SslCiphers => GetValue<string>("ssl_ciphers");

    public string? SslCipherSuites => GetValue<string>("ssl_ciphersuites");

    public override OVSParentReference GetParentReference()
    {
        return new OVSParentReference(
            OVNSouthboundTableNames.Global,
            Optional(GetValue<Guid>("__parentId")).Map(i => i.ToString("D")),
            "ssl");
    }
}
