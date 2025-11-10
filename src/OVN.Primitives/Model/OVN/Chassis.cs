using static LanguageExt.Prelude;

namespace Dbosoft.OVN.Model.OVN;

public record Chassis : OVSTableRecord, IHasParentReference, IOVSEntityWithName
{
    public new static readonly IDictionary<string, OVSFieldMetadata>
        Columns = new Dictionary<string, OVSFieldMetadata>(OVSTableRecord.Columns)
        {
            { "chassis_name", OVSValue<string>.Metadata() },
            { "priority", OVSValue<short>.Metadata() },
        };

    public string? Name => GetValue<string>("chassis_name");

    public short? Priority => GetValue<short>("priority");

    public OVSParentReference GetParentReference()
    {
        return new OVSParentReference(
            OVNTableNames.ChassisGroups,
            Optional(GetValue<Guid>("__parentId")).Map(i => i.ToString("D")),
            "ha_chassis");
    }
}
