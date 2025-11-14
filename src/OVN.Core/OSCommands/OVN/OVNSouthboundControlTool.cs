namespace Dbosoft.OVN.OSCommands.OVN;

public class OVNSouthboundControlTool(
    ISystemEnvironment systemEnvironment,
    OvsDbConnection dbConnection)
    : OVSControlToolBase(systemEnvironment, dbConnection, OVNCommands.SouthboundControl);
