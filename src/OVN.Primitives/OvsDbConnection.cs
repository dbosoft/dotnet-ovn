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

    /// <summary>
    /// Specifies a local pipe-based database connection.
    /// </summary>
    public OvsDbConnection(OvsFile pipeFile)
    {
        PipeFile = pipeFile;
    }

    /// <summary>
    /// Specifies a network-based database connection.
    /// </summary>
    /// <remarks>
    /// Settings <paramref name="ssl"/> to <see langword="true"/> is only supported
    /// for outgoing connections. The SSL configuration must be added to the database.
    /// Use a different constructor when configuring the connection for clients
    /// or tools.
    /// </remarks>
    public OvsDbConnection(string address, int port, bool ssl = false)
    {
        Address = address;
        Port = port;
        Ssl = ssl;
    }

    /// <summary>
    /// Specifies an SSL-protected database connection.
    /// </summary>
    /// <remarks>
    /// This kind of connection is only intended for clients or tools. The
    /// private key is used for client authentication.
    /// </remarks>
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
