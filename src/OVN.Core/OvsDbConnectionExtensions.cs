namespace Dbosoft.OVN;

/// <summary>
/// Extensions for OvsDbConnection
/// </summary>
public static class OvsDbConnectionExtensions
{
    /// <summary>
    /// Gets connection string for a OVS command
    /// </summary>
    /// <param name="connection">connection</param>
    /// <param name="fileSystem">File system abstraction</param>
    /// <param name="passive">generate as passive (listening) connection.</param>
    /// <returns>the connection string</returns>
    public static string GetCommandString(this OvsDbConnection connection, IFileSystem fileSystem, bool passive)
    {
        var passivePrefix = passive ? "p" : "";

        if (connection.PipeFile != null)
        {
            var path = fileSystem.ResolveOvsFilePath(connection.PipeFile);
            return $"{passivePrefix}unix:{path}";
        }

        var portType = connection.Ssl ? "ssl" : "tcp";


        var portAndAddress = passive ? $"{connection.Port}:{connection.Address}" : $"{connection.Address}:{connection.Port}";

        return $"{passivePrefix}{portType}:{portAndAddress}";
    }
}