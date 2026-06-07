using static LanguageExt.Prelude;

namespace Dbosoft.OVN.Model.OVN;

public record NorthboundSsl : OVSSslTableRecord
{
    public new static readonly IDictionary<string, OVSFieldMetadata>
        Columns = new Dictionary<string, OVSFieldMetadata>(OVSSslTableRecord.Columns)
        {
            { "ssl_protocols", OVSValue<string>.Metadata() },
            { "ssl_ciphers", OVSValue<string>.Metadata() },
            { "ssl_ciphersuites", OVSValue<string>.Metadata() },
        };

    // ssl_protocols/ssl_ciphers/ssl_ciphersuites exist in the OVN SSL table but
    // not in the Open_vSwitch (switch) SSL schema, so they are declared here in
    // the OVN-specific record instead of the shared OVSSslTableRecord.

    public string? SslProtocols => GetValue<string>("ssl_protocols");

    public string? SslCiphers => GetValue<string>("ssl_ciphers");

    public string? SslCipherSuites => GetValue<string>("ssl_ciphersuites");

    public override OVSParentReference GetParentReference()
    {
        return new OVSParentReference(
            OVNTableNames.Global,
            Optional(GetValue<Guid>("__parentId")).Map(i => i.ToString("D")),
            "ssl");
    }
}
