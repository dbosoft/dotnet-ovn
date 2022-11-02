using JetBrains.Annotations;

namespace Dbosoft.OVN.Model.OVN;

[PublicAPI]
public record PlannedRouter : OVSEntity, IOVSEntityWithName
{
    public new static readonly IDictionary<string, OVSFieldMetadata>
        Columns = new Dictionary<string, OVSFieldMetadata>(OVSEntity.Columns)
        {
            { "name", OVSValue<string>.Metadata() }
        };

    public string? Name
    {
        get => GetValue<string>("name");
        init => SetValue("name", value);
    }
}