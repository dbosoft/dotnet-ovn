namespace Dbosoft.OVN.Model;

public abstract record OVSSslTableRecord : OVSTableRecord, IHasParentReference, IOVSEntityWithName
{
    public new static readonly IDictionary<string, OVSFieldMetadata>
        Columns = new Dictionary<string, OVSFieldMetadata>(OVSTableRecord.Columns)
        {
            { "private_key", OVSValue<string>.Metadata() },
            { "certificate", OVSValue<string>.Metadata() },
            { "ca_cert", OVSValue<string>.Metadata() },
        };

    public string? PrivateKey => GetValue<string>("private_key");

    public string? Certificate => GetValue<string>("certificate");

    public string? CaCertificate => GetValue<string>("ca_cert");

    public string? Name => "SSL";

    public abstract OVSParentReference GetParentReference();
}
