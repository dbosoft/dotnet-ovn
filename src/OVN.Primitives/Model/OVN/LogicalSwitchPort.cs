using JetBrains.Annotations;
using LanguageExt;

using static LanguageExt.Prelude;

namespace Dbosoft.OVN.Model.OVN;

[PublicAPI]
public record LogicalSwitchPort : OVSTableRecord, IHasParentReference, IOVSEntityWithName
{
    public new static readonly IDictionary<string, OVSFieldMetadata>
        Columns = new Dictionary<string, OVSFieldMetadata>(OVSTableRecord.Columns)
        {
            { "name", OVSValue<string>.Metadata(true) },
            { "type", OVSValue<string>.Metadata(true) },
            { "addresses", OVSSet<string>.Metadata() },
            { "dhcpv4_options", OVSSet<Guid>.Metadata() },
            { "port_security", OVSSet<string>.Metadata() },
            { "options", OVSMap<string>.Metadata(true) },
            { "tag", OVSValue<int>.Metadata() }

        };

    public Seq<string> Addresses => GetSet<string>("addresses");

    public Seq<Guid> DHCPOptionsV4 => GetSet<Guid>("dhcpv4_options");

    public Map<string, string> Options => GetMap<string>("options");

    public string? Type => GetValue<string>("type");

    public Guid? ParentId => GetValue<Guid>("__parentId");

    public OVSParentReference GetParentReference()
    {
        return new OVSParentReference(
            OVNTableNames.LogicalSwitch,
            Optional(GetValue<Guid>("__parentId")).Map(i => i.ToString("D")),
            "Ports");
    }

    public string? Name
    {
        get => GetValue<string>("name");
        init => SetValue("name", value);
    }
}