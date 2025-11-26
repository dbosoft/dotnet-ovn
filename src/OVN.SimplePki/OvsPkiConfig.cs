namespace Dbosoft.OVN.SimplePki;

/// <summary>
/// Contains the certificates and private key for an OVS SSL configuration.
/// </summary>
public record OvsPkiConfig(
    string PrivateKey,
    string Certificate,
    string CaCertificate);
