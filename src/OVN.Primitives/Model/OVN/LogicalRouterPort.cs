using JetBrains.Annotations;
using LanguageExt;

using static LanguageExt.Prelude;

namespace Dbosoft.OVN.Model.OVN;

[PublicAPI]
public record LogicalRouterPort : OVSTableRecord, IOVSEntityWithName, IHasParentReference
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

    public Guid? ParentId => GetValue<Guid>("__parentId");

    public OVSParentReference GetParentReference()
    {
        return new OVSParentReference(
            OVNTableNames.LogicalRouter,
            Optional(GetValue<Guid>("__parentId")).Map(i => i.ToString("D")),
            "Ports");
    }
}
