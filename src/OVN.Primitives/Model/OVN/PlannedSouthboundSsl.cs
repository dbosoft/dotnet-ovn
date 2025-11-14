namespace Dbosoft.OVN.Model.OVN;

public record PlannedSouthboundSsl : PlannedOvsSsl
{
    public override OVSParentReference GetParentReference()
    {
        return new OVSParentReference(OVNSouthboundTableNames.Global, ".", "ssl");
    }
}
