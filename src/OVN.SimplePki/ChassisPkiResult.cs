namespace Dbosoft.OVN.SimplePki;

public record ChassisPkiResult(
    string PrivateKey,
    string Certificate,
    string CaCertificate);
