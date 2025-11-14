namespace Dbosoft.OVN.SimplePki;

public interface IPkiService
{
    Task InitializeAsync();

    Task<ChassisPkiResult> GenerateChassisPkiAsync(string chassisName);
}
