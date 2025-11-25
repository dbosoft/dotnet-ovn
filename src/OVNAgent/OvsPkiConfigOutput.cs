namespace Dbosoft.OVNAgent;

public class OvsPkiConfigOutput
{
    public required string PrivateKey { get; init; }

    public required string Certificate { get; init; }

    public required string CaCertificate { get; init; }
}
