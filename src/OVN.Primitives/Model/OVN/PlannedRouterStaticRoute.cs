using JetBrains.Annotations;

namespace Dbosoft.OVN.Model.OVN;

[PublicAPI]
public record PlannedRouterStaticRoute(string RouterName) : OVSEntity, IOVSEntityWithName, IHasParentReference
{
    [UsedImplicitly] public new static readonly IDictionary<string, OVSFieldMetadata>
        Columns = new Dictionary<string, OVSFieldMetadata>(OVSEntity.Columns)
        {
            { "ip_prefix", OVSValue<string>.Metadata() },
            { "nexthop", OVSValue<string>.Metadata() }
        };

    public string? IpPrefix
    {
        get => GetValue<string>("ip_prefix");
        init => SetValue("ip_prefix", value);
    }

    public string? NextHop
    {
        // ReSharper disable once StringLiteralTypo
        get => GetValue<string>("nexthop");
        // ReSharper disable once StringLiteralTypo
        init => SetValue("nexthop", value);
    }


    public OVSParentReference? GetParentReference()
    {
        return new OVSParentReference(OVNTableNames.LogicalRouter, RouterName, "static_routes");
    }

    public string Name =>
        $"router:{RouterName}, ip_prefix:{IpPrefix}";
}