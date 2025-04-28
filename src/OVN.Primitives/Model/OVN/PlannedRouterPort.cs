using JetBrains.Annotations;
using LanguageExt;

namespace Dbosoft.OVN.Model.OVN;

[PublicAPI]
public record PlannedRouterPort(string RouterName) : OVSEntity, IOVSEntityWithName, IHasParentReference
{
    public new static readonly IDictionary<string, OVSFieldMetadata>
        Columns = new Dictionary<string, OVSFieldMetadata>(OVSEntity.Columns)
        {
            { "name", OVSValue<string>.Metadata() },
            { "mac", OVSValue<string>.Metadata() },
            { "peer", OVSValue<string>.Metadata() },
            { "networks", OVSSet<string>.Metadata() },
            { "ha_chassis_group", OVSSet<Guid>.Metadata() },
            { "options", OVSMap<string>.Metadata() },
        };

    public string? MacAddress
    {
        get => GetValue<string>("mac");
        init => SetValue("mac", value);
    }
    
    public Seq<Guid> ChassisGroupRef
    {
        get => GetSet<Guid>("ha_chassis_group");
        init => SetSet("ha_chassis_group", value);
    }

    public Seq<string> Networks
    {
        get => GetSet<string>("networks");
        init => SetSet("networks", value);
    }

    public OVSParentReference? GetParentReference()
    {
        return new OVSParentReference(OVNTableNames.LogicalRouter, RouterName, "Ports");
    }

    public string? Name
    {
        get => GetValue<string>("name");
        init => SetValue("name", value);
    }

    public string? Peer
    {
        get => GetValue<string>("peer");
        init => SetValue("peer", value);
    }

    public Map<string, string> Options
    {
        get => GetMap<string>("options");
        init => SetMap("options", value);
    }
}
