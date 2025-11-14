using Dbosoft.OVN.Logging;
using LanguageExt;

namespace Dbosoft.OVN.OSCommands.OVS;

public record OVSDbSettings(
    OvsDbConnection Connection,
    OvsFile DBFile,
    OvsFile SchemaFile,
    OvsFile ControlFile,
    OvsFile LogFile,
    OvsLoggingSettings LoggingSettings,
    string DatabaseName,
    string GlobalTableName,
    bool AllowAttach,
    bool UseRemotesFromDatabase);
