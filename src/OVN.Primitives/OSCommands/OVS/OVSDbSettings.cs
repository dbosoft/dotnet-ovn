using Dbosoft.OVN.Logging;

namespace Dbosoft.OVN.OSCommands.OVS;

public record OVSDbSettings(
    OvsDbConnection Connection,
    OvsFile DBFile,
    OvsFile SchemaFile,
    OvsFile ControlFile,
    OvsFile LogFile,
    OvsLoggingSettings LoggingSettings,
    bool AllowAttach);
