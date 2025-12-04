namespace Dbosoft.OVN.Model;

public abstract record PlannedOvsSsl : OVSEntity, IOVSEntityWithName, IHasParentReference
{
    public new static readonly IDictionary<string, OVSFieldMetadata>
        Columns = new Dictionary<string, OVSFieldMetadata>(OVSEntity.Columns)
        {
            { "private_key", OVSValue<string>.Metadata() },
            { "certificate", OVSValue<string>.Metadata() },
            { "ca_cert", OVSValue<string>.Metadata() },
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

    /// <summary>
    /// The name is fixed to <c>SSL</c> as only one SSL record is allowed.
    /// </summary>
    /// <remarks>
    /// The fixed name allows us to reuse the existing CRUD logic.
    /// </remarks>
    public string? Name => "SSL";

    public abstract OVSParentReference GetParentReference();
}
