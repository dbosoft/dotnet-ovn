using Dbosoft.OVN.Logging;

namespace Dbosoft.OVN.OSCommands.OVN;

public record OVNControllerSettings(
    OvsDbConnection OvsDbConnection,
    OvsLoggingSettings LoggingSettings,
    bool AllowAttach);
