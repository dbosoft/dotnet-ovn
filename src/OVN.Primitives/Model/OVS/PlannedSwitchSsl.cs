namespace Dbosoft.OVN.Model.OVS;

public record PlannedSwitchSsl : PlannedOvsSsl
{
    public override OVSParentReference GetParentReference()
    {
        return new OVSParentReference(OVSTableNames.Global, ".", "ssl");
    }
}
