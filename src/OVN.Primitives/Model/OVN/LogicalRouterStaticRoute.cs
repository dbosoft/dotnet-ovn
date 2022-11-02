using JetBrains.Annotations;

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

    private Guid? ParentId => GetValue<Guid>("__parentId");

    private string? RouterName => ExternalIds.ContainsKey("router_name") ? ExternalIds["router_name"] : null;
    public OVSParentReference? GetParentReference()
    {
        if (!ParentId.HasValue)
            return null;

        return new OVSParentReference(OVNTableNames.LogicalRouter,
            ParentId.GetValueOrDefault().ToString("D"), "static_routes");
    }

    public string Name =>
        $"router:{RouterName}, ip_prefix:{IpPrefix}";
}