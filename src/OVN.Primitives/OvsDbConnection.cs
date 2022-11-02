using JetBrains.Annotations;

namespace Dbosoft.OVN;

[PublicAPI]
public record OvsDbConnection
{
    public readonly string? Address;
    public readonly OvsFile? PipeFile;
    public readonly int Port;
    public readonly bool Ssl;

    public OvsDbConnection(OvsFile pipeFile)
    {
        PipeFile = pipeFile;
    }

    public OvsDbConnection(string address, int port, bool ssl = false)
    {
        Address = address;
        Port = port;
        Ssl = ssl;
    }
    
}