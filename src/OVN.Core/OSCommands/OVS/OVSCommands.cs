namespace Dbosoft.OVN.OSCommands.OVS;

public static class OVSCommands
{
    // ReSharper disable StringLiteralTypo
    public static readonly OvsFile DBServer = new("usr/sbin", "ovsdb-server", true);
    public static readonly OvsFile DBClient = new("usr/bin", "ovsdb-client", true);
    public static readonly OvsFile DBTool = new("usr/bin", "ovsdb-tool", true);
    public static readonly OvsFile AppControl = new("usr/bin", "ovs-appctl", true);
    public static readonly OvsFile VSwitchDemon = new("usr/sbin", "ovs-vswitchd", true);
    public static readonly OvsFile VSwitchControl = new("usr/bin", "ovs-vsctl", true);
    // ReSharper restore StringLiteralTypo
}