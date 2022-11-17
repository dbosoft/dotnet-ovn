namespace Dbosoft.OVN.OSCommands.OVN;

public static class OVNCommands
{
    public static readonly OvsFile NorthboundDemon = new("usr/bin", "ovn-northd", true);
    public static readonly OvsFile NorthboundControl = new("usr/bin", "ovn-nbctl", true);
    public static readonly OvsFile OVNController = new("usr/bin", "ovn-controller", true);
    public static readonly OvsFile AppControl = new("usr/bin", "ovn-appctl", true);
}