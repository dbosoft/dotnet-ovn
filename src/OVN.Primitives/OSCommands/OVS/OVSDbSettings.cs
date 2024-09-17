namespace Dbosoft.OVN.OSCommands.OVS;

public record OVSDbSettings(
    OvsDbConnection Connection,
    OvsFile DBFile,
    OvsFile SchemaFile,
    OvsFile ControlFile,
    OvsFile LogFile,
    string LogLevel,
    bool AllowAttach);