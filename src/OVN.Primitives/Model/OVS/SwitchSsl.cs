using static LanguageExt.Prelude;

namespace Dbosoft.OVN.Model.OVS;

public record SwitchSsl : OVSSslTableRecord
{
    public override OVSParentReference GetParentReference()
    {
        return new OVSParentReference(
            OVSTableNames.Global,
            Optional(GetValue<Guid>("__parentId")).Map(i => i.ToString("D")),
            "ssl");
    }
}
