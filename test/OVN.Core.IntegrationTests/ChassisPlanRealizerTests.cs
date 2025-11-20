using System.Net;
using AwesomeAssertions;
using Dbosoft.OVN.SimplePki;
using LanguageExt.Common;
using Xunit.Abstractions;

using static Dbosoft.OVN.Core.IntegrationTests.HashHelper;

namespace Dbosoft.OVN.Core.IntegrationTests;

public class ChassisPlanRealizerTests : OvsControlToolTestBase
{
    private readonly IPkiService _pkiService;

    public ChassisPlanRealizerTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
        _pkiService = new PkiService(SystemEnvironment);
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await _pkiService.InitializeAsync();
    }

    [Fact]
    public async Task ApplyChassisPlan_NewPlan_IsSuccessful()
    {
        var initialChassisPki = await _pkiService.GenerateChassisPkiAsync("test-chassis");
        await ApplyChassisPlan(CreateChassisPlan(initialChassisPki));

        await VerifyDatabase();

        var configDirectory = GetDataDirectoryInfo().Should().ContainDirectory("etc")
            .Which.Should().ContainDirectory("openvswitch")
            .Subject;
        configDirectory.Should().ContainFile($"cacert_{ComputeSha256(initialChassisPki.CaCertificate)}.pem");
        configDirectory.Should().ContainFile($"cert_{ComputeSha256(initialChassisPki.Certificate)}.pem");
        configDirectory.Should().ContainDirectory("private")
            .Which.Should().ContainFile($"key_{ComputeSha256(initialChassisPki.PrivateKey)}.pem");
    }

    [Fact]
    public async Task ApplyChassisPlan_UpdatedPlan_IsSuccessful()
    {
        var initialChassisPki = await _pkiService.GenerateChassisPkiAsync("test-chassis");
        await ApplyChassisPlan(CreateChassisPlan(initialChassisPki));

        var updatedChassisPki = await _pkiService.GenerateChassisPkiAsync("test-chassis");
        var updatedPlan = new ChassisPlan("chassis-1")
            .SetSouthboundDatabase(IPAddress.Parse("203.0.113.100"))
            .SetSwitchSsl(updatedChassisPki.PrivateKey, updatedChassisPki.Certificate, updatedChassisPki.CaCertificate)
            .AddGeneveTunnelEndpoint(IPAddress.Parse("203.0.113.101"))
            .AddBridgeMapping("extern", "br-outside")
            .AddBridgeMapping("other-net", "br-other-net");

        await ApplyChassisPlan(updatedPlan);

        await VerifyDatabase();

        var configDirectory = GetDataDirectoryInfo().Should().ContainDirectory("etc")
            .Which.Should().ContainDirectory("openvswitch")
            .Subject;
        // The CA certificate does not change as we use the same PKI
        configDirectory.Should().ContainFile($"cacert_{ComputeSha256(initialChassisPki.CaCertificate)}.pem");
        configDirectory.Should().ContainFile($"cert_{ComputeSha256(updatedChassisPki.Certificate)}.pem");
        configDirectory.Should().NotContainFile($"cert_{ComputeSha256(initialChassisPki.Certificate)}.pem");
        var privateConfigDirectory = configDirectory.Should().ContainDirectory("private").Subject;
        privateConfigDirectory.Should().ContainFile($"key_{ComputeSha256(updatedChassisPki.PrivateKey)}.pem");
        privateConfigDirectory.Should().NotContainFile($"key_{ComputeSha256(initialChassisPki.PrivateKey)}.pem");
    }

    [Fact]
    public async Task ApplyChassisPlan_ChassisIdIsChanged_Fails()
    {
        var initialChassisPki = await _pkiService.GenerateChassisPkiAsync("test-chassis");
        await ApplyChassisPlan(CreateChassisPlan(initialChassisPki));

        var updatedChassisPki = await _pkiService.GenerateChassisPkiAsync("test-chassis");
        var updatedPlan = new ChassisPlan("chassis-2")
            .SetSouthboundDatabase(IPAddress.Parse("203.0.113.1"))
            .SetSwitchSsl(updatedChassisPki.PrivateKey, updatedChassisPki.Certificate, updatedChassisPki.CaCertificate)
            .AddGeneveTunnelEndpoint(IPAddress.Parse("203.0.113.2"))
            .AddBridgeMapping("extern", "br-extern");

        var act = () => ApplyChassisPlan(updatedPlan);
        await act.Should().ThrowAsync<ErrorException>()
            .WithMessage("*A different chassis ID ('chassis-1') is already configured*");
    }

    private async Task ApplyChassisPlan(ChassisPlan chassisPlan)
    {
        var realizer = new ChassisPlanRealizer(SystemEnvironment, ControlTool);

        (await realizer.ApplyChassisPlan(chassisPlan)).ThrowIfLeft();
    }

    private ChassisPlan CreateChassisPlan(ChassisPkiResult chassisPki) =>
        new ChassisPlan("chassis-1")
            .SetSwitchSsl(chassisPki.PrivateKey, chassisPki.Certificate, chassisPki.CaCertificate)
            .SetSouthboundDatabase(IPAddress.Parse("203.0.113.1"))
            .AddGeneveTunnelEndpoint(IPAddress.Parse("203.0.113.2"))
            .AddBridgeMapping("extern", "br-extern");
}
