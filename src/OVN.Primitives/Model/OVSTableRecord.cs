using JetBrains.Annotations;

namespace Dbosoft.OVN.Model;

[PublicAPI]
public record OVSTableRecord : OVSEntity
{
    public new static readonly IDictionary<string, OVSFieldMetadata>
        Columns = new Dictionary<string, OVSFieldMetadata>(OVSEntity.Columns)
        {
            { "_uuid", OVSValue<Guid>.Metadata() }
        };

    public Guid Id => GetValue<Guid>("_uuid");
}