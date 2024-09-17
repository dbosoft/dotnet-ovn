namespace Dbosoft.OVN.OSCommands.OVN;

public record OVNControllerSettings(OvsDbConnection OvsDbConnection, string LogFileLevel, bool AllowAttach);