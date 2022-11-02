using LanguageExt;

namespace Dbosoft.OVN.Model.OVN;

public record ChassisGroup : OVSTableRecord, IOVSEntityWithName
{
    public new static readonly IDictionary<string, OVSFieldMetadata>
        Columns = new Dictionary<string, OVSFieldMetadata>(OVSTableRecord.Columns)
        {
            { "name", OVSValue<string>.Metadata() },
            { "ha_chassis", OVSSet<Guid>.Metadata() }
        };
    
    public string? Name => GetValue<string>("name");
    public Seq<Guid>? Chassis => GetSet<Guid>("ha_chassis");
}

public record Chassis : OVSTableRecord
{
    public new static readonly IDictionary<string, OVSFieldMetadata>
        Columns = new Dictionary<string, OVSFieldMetadata>(OVSTableRecord.Columns)
        {
            { "chassis_name", OVSValue<string>.Metadata() },
        };
    

}