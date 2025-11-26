using LanguageExt;

namespace Dbosoft.OVN.Model.OVN;

public record ChassisGroup : OVSTableRecord, IOVSEntityWithName, IHasOVSReferences<Chassis>
{
    public new static readonly IDictionary<string, OVSFieldMetadata>
        Columns = new Dictionary<string, OVSFieldMetadata>(OVSTableRecord.Columns)
        {
            { "name", OVSValue<string>.Metadata() },
            { "ha_chassis", OVSReference.Metadata() },
        };
    
    public string? Name => GetValue<string>("name");

    public Seq<Guid> Chassis => GetReference("ha_chassis");

    Seq<Guid> IHasOVSReferences<Chassis>.GetOvsReferences() => Chassis;
}
