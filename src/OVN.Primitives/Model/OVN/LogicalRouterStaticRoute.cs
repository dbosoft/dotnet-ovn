using JetBrains.Annotations;

using static LanguageExt.Prelude;

namespace Dbosoft.OVN.Model.OVN;

[PublicAPI]
public record LogicalRouterStaticRoute : OVSTableRecord, IOVSEntityWithName, IHasParentReference
{
    public new static readonly IDictionary<string, OVSFieldMetadata>
        Columns = new Dictionary<string, OVSFieldMetadata>(OVSTableRecord.Columns)
        {
            { "ip_prefix", OVSValue<string>.Metadata() },
            { "nexthop", OVSValue<string>.Metadata() }
        };

    public string? IpPrefix => GetValue<string>("ip_prefix");

    public string? Nexthop => GetValue<string>("nexthop");

    private string? RouterName => ExternalIds.ContainsKey("router_name") ? ExternalIds["router_name"] : null;

    public string Name => $"router:{RouterName}, ip_prefix:{IpPrefix}";

    public OVSParentReference GetParentReference() =>
        new(OVNTableNames.LogicalRouter,
            Optional(GetValue<Guid>("__parentId")).Map(i => i.ToString("D")),
            "static_routes");
}
