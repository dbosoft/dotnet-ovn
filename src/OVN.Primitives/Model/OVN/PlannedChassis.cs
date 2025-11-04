using JetBrains.Annotations;

namespace Dbosoft.OVN.Model.OVN;

[PublicAPI]
public record PlannedChassis(string ChassisGroupName) : OVSEntity, IOVSEntityWithName, IHasParentReference
{
    public new static readonly IDictionary<string, OVSFieldMetadata>
        Columns = new Dictionary<string, OVSFieldMetadata>(OVSEntity.Columns)
        {
            { "chassis_name", OVSValue<string>.Metadata() },
            { "priority", OVSValue<short>.Metadata() },
        };

    public OVSParentReference? GetParentReference()
    {
        return new OVSParentReference(OVNTableNames.ChassisGroups, ChassisGroupName, "ha_chassis");
    }

    public string? Name
    {
        get => GetValue<string>("chassis_name");
        init => SetValue("chassis_name", value);
    }

    public short? Priority
    {
        get => GetValue<short>("priority");
        init => SetValue("priority", value);
    }
}
