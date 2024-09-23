using Dbosoft.OVN.Logging;

namespace Dbosoft.OVN.OSCommands.OVN;

public record NorthDSettings(
    OvsDbConnection NorthDbConnection,
    OvsDbConnection SouthDBConnection,
    OvsLoggingSettings LoggingSettings,
    bool AllowAttach);
