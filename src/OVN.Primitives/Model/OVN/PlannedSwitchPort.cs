using JetBrains.Annotations;
using LanguageExt;

namespace Dbosoft.OVN.Model.OVN;

[PublicAPI]
public record PlannedSwitchPort(string SwitchName) : OVSEntity, IHasParentReference, IOVSEntityWithName
{
    public new static readonly IDictionary<string, OVSFieldMetadata>
        Columns = new Dictionary<string, OVSFieldMetadata>(OVSEntity.Columns)
        {
            { "name", OVSValue<string>.Metadata(true) },
            { "addresses", OVSSet<string>.Metadata() },
            { "dhcpv4_options", OVSSet<Guid>.Metadata() },
            { "port_security", OVSSet<string>.Metadata() },
            { "options", OVSMap<string>.Metadata() },
            { "type", OVSValue<string>.Metadata() },
            { "tag", OVSValue<int>.Metadata() }
        };

    public Seq<string> Addresses
    {
        get => GetSet<string>("addresses");
        init => SetSet("addresses", value);
    }

    public Seq<string> PortSecurity
    {
        get => GetSet<string>("port_security");
        init => SetSet("port_security", value);
    }

    public Map<string, string> Options
    {
        get => GetMap<string>("options");
        init => SetMap("options", value);
    }

    public string? Type
    {
        get => GetValue<string>("type");
        init => SetValue("type", value);
    }

    public Seq<Guid> DHCPOptionsRefV4
    {
        get => GetSet<Guid>("dhcpv4_options");
        init => SetSet("dhcpv4_options", value);
    }

    public OVSParentReference GetParentReference() =>
        new(OVNTableNames.LogicalSwitch, SwitchName, "Ports");

    public string? Name
    {
        get => GetValue<string>("name");
        init => SetValue("name", value);
    }

    public int? Tag
    {
        get => GetValue<int>("tag");
        init => SetValue("tag", value);
    }
}
