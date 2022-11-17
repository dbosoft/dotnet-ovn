using System.Net.Sockets;
using LanguageExt;
using LanguageExt.Common;

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
    
    public static async Task<Either<Error, bool>> WaitForDbSocket(this OvsDbConnection connection, ISysEnvironment sysEnv, CancellationToken cancellationToken)
    {
        return await Prelude.TryAsync(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (connection.PipeFile != null)
                {
                    if (sysEnv.FileSystem.FileExists(connection.PipeFile))
                        return true;
                }
                else
                {
                    using var tcpClient = new TcpClient();
                    try
                    {
                        await tcpClient.ConnectAsync(
                            connection.Address ?? "127.0.0.1",
                            connection.Port, cancellationToken);
                        return true;
                    }
                    catch (Exception)
                    {
                        // ignored, port closed
                    }
                }

                await Task.Delay(500, cancellationToken);
            }

            return false;
        }).ToEither(l => Error.New(l)).Map(_ => true);
    }
}