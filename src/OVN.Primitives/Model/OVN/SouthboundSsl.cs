using static LanguageExt.Prelude;

namespace Dbosoft.OVN.Model.OVN;

public record SouthboundSsl : OVSSsl
{
    //public new static readonly IDictionary<string, OVSFieldMetadata> Columns = OVSSsl.Columns;

    public override OVSParentReference GetParentReference()
    {
        return new OVSParentReference(
            OVNSouthboundTableNames.Global,
            Optional(GetValue<Guid>("__parentId")).Map(i => i.ToString("D")),
            "ssl");
    }
}
