namespace Dbosoft.OVN.SimplePki;

public interface IPkiService
{
    Task InitializeAsync();

    Task<OvsPkiConfig> GenerateChassisPkiAsync(string chassisName);
}
