using Dbosoft.OVN.Logging;

namespace Dbosoft.OVN.OSCommands.OVS;

public record VSwitchDSettings(
    OvsDbConnection DbConnection,
    OvsLoggingSettings LoggingSettings,
    bool AllowAttach);
