using static LanguageExt.Prelude;

namespace Dbosoft.OVN.Model.OVN;

public record SouthboundConnection : OVSTableRecord, IHasParentReference, IOVSEntityWithName
{
    public new static readonly IDictionary<string, OVSFieldMetadata>
        Columns = new Dictionary<string, OVSFieldMetadata>(OVSTableRecord.Columns)
        {
            { "target", OVSValue<string>.Metadata() },
        };

    public string? Target => GetValue<string>("target");

    public string? Name => Target;

    public OVSParentReference GetParentReference()
    {
        return new OVSParentReference(
            OVNSouthboundTableNames.Global,
            Optional(GetValue<Guid>("__parentId")).Map(i => i.ToString("D")),
            "Connections");
    }
}
