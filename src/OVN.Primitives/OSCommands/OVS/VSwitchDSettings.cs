namespace Dbosoft.OVN.OSCommands.OVS;

public record VSwitchDSettings(OvsDbConnection DbConnection, string LogFileLevel, bool AllowAttach);