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

    /// <summary>
    /// The name is fixed to <c>SSL</c> as only one SSL record is allowed.
    /// </summary>
    /// <remarks>
    /// The fixed name allows us to reuse the existing CRUD logic.
    /// </remarks>
    public string? Name => "SSL";

    public abstract OVSParentReference GetParentReference();
}
