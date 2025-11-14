using JetBrains.Annotations;

namespace Dbosoft.OVN;

[PublicAPI]
public record OvsDbConnection
{
    public readonly string? Address;
    public readonly OvsFile? PipeFile;
    public readonly int Port;
    public readonly bool Ssl;
    public readonly OvsFile? PrivateKeyFile;
    public readonly OvsFile? CertificateFile;
    public readonly OvsFile? CaCertificateFile;

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

    public OvsDbConnection(
        string address,
        int port,
        OvsFile privateKeyFile,
        OvsFile certificateFile,
        OvsFile caCertificateFile)
    {
        Address = address;
        Port = port;
        Ssl = true;
        PrivateKeyFile = privateKeyFile;
        CertificateFile = certificateFile;
        CaCertificateFile = caCertificateFile;
    }
}
