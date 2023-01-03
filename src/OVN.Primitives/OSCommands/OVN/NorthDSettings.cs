namespace Dbosoft.OVN.OSCommands.OVN;

public record NorthDSettings(OvsDbConnection NorthDbConnection, OvsDbConnection SouthDBConnection, bool AllowAttach );