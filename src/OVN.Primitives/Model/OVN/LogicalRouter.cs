using LanguageExt;

namespace Dbosoft.OVN.Model.OVN;

public record LogicalRouter : OVSTableRecord, IOVSEntityWithName
{
    public new static readonly IDictionary<string, OVSFieldMetadata>
        Columns = new Dictionary<string, OVSFieldMetadata>(OVSTableRecord.Columns)
        {
            { "name", OVSValue<string>.Metadata() },
            { "static_routes", OVSReference.Metadata() },
            { "nat", OVSReference.Metadata() }
        };

    public Seq<Guid> StaticRoutes => GetReference("static_routes");
    public Seq<Guid> NATRules => GetReference("nat");

    public string? Name => GetValue<string>("name");
}