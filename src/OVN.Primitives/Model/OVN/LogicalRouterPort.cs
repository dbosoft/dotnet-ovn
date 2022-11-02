using JetBrains.Annotations;
using LanguageExt;

namespace Dbosoft.OVN.Model.OVN;

[PublicAPI]
public record LogicalRouterPort : OVSTableRecord, IOVSEntityWithName
{
    public new static readonly IDictionary<string, OVSFieldMetadata>
        Columns = new Dictionary<string, OVSFieldMetadata>(OVSTableRecord.Columns)
        {
            { "name", OVSValue<string>.Metadata() },
            { "mac", OVSValue<string>.Metadata() },
            { "ha_chassis_group", OVSSet<Guid>.Metadata() },
            { "networks", OVSSet<string>.Metadata() }
        };

    public string? MacAddress => GetValue<string>("mac");
    public Seq<Guid> ChassisGroupRef => GetSet<Guid>("ha_chassis_group");
    public Seq<string> Networks => GetSet<string>("networks");

    public string? Name => GetValue<string>("name");
}