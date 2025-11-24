using System.Net;
using AwesomeAssertions;
using Dbosoft.OVN.Model.OVN;
using Dbosoft.OVN.OSCommands.OVN;
using Dbosoft.OVN.SimplePki;
using Xunit.Abstractions;

using static Dbosoft.OVN.Core.IntegrationTests.HashHelper;

namespace Dbosoft.OVN.Core.IntegrationTests;

public class ClusterPlanSouthboundRealizerTests : OvnSouthboundControlToolTestBase
{
    private readonly IPkiService _pkiService;
    private readonly OvsFile _clientPrivateKey = new("/etc/dotnet-ovn/test-client", "key.pem");
    private readonly OvsFile _clientCertificate = new("/etc/dotnet-ovn/test-client", "cert.pem");
    private readonly OvsFile _clientCaCertificate = new("/etc/dotnet-ovn/test-client", "ca-cert.pem");

    public ClusterPlanSouthboundRealizerTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
        _pkiService = new PkiService(SystemEnvironment);
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await _pkiService.InitializeAsync();

        var clientPki = await _pkiService.GenerateChassisPkiAsync("test-client");
        
        SystemEnvironment.FileSystem.EnsurePathForFileExists(_clientPrivateKey);
        await SystemEnvironment.FileSystem.WriteFileAsync(_clientPrivateKey, clientPki.PrivateKey);
        SystemEnvironment.FileSystem.EnsurePathForFileExists(_clientCertificate);
        await SystemEnvironment.FileSystem.WriteFileAsync(_clientCertificate, clientPki.Certificate);
        SystemEnvironment.FileSystem.EnsurePathForFileExists(_clientCaCertificate);
        await SystemEnvironment.FileSystem.WriteFileAsync(_clientCaCertificate, clientPki.CaCertificate);
    }

    [Fact]
    public async Task ApplyClusterPlan_NewPlan_IsSuccessful()
    {
        var initialChassisPki = await _pkiService.GenerateChassisPkiAsync("test-chassis");
        await ApplyClusterPlan(CreateClusterPlan(initialChassisPki));

        await VerifyDatabase();

        var configDirectory = GetDataDirectoryInfo().Should().ContainDirectory("etc")
            .Which.Should().ContainDirectory("openvswitch")
            .Subject;
        configDirectory.Should().ContainFile($"cacert_{ComputeSha256(initialChassisPki.CaCertificate)}.pem");
        configDirectory.Should().ContainFile($"cert_{ComputeSha256(initialChassisPki.Certificate)}.pem");
        configDirectory.Should().ContainDirectory("private")
            .Which.Should().ContainFile($"key_{ComputeSha256(initialChassisPki.PrivateKey)}.pem");

        await TestConnection(42421, false);
        await TestConnection(42422, true);
    }

    [Fact]
    public async Task ApplyClusterPlan_UpdatedPlan_IsSuccessful()
    {
        var initialChassisPki = await _pkiService.GenerateChassisPkiAsync("test-chassis");
        await ApplyClusterPlan(CreateClusterPlan(initialChassisPki));

        var updatedChassisPki = await _pkiService.GenerateChassisPkiAsync("test-chassis");
        var updatedPlan = new ClusterPlan()
            .SetSouthboundSsl(updatedChassisPki.PrivateKey, updatedChassisPki.Certificate, updatedChassisPki.CaCertificate)
            .AddSouthboundConnection(52421)
            .AddSouthboundConnection(52422, true);

        await ApplyClusterPlan(updatedPlan);

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

        await TestConnection(52421, false);
        await TestConnection(52422, true);
    }

    private async Task ApplyClusterPlan(ClusterPlan clusterPlan)
    {
        var realizer = new ClusterPlanSouthboundRealizer(SystemEnvironment, ControlTool);

        var either = await realizer.ApplyClusterPlan(clusterPlan);
        either.ThrowIfLeft();

        // The ovsdb-server writes status information for the connections
        // into the connection table. We just wait a bit here to ensure that
        // the status information has been written. Otherwise, the tests can
        // randomly fail depending on whether the status information is present
        // or not in the database and hence included in the database snapshot.
        await Task.Delay(2000);
    }

    private ClusterPlan CreateClusterPlan(ChassisPkiResult chassisPki) =>
        new ClusterPlan()
            .SetSouthboundSsl(chassisPki.PrivateKey, chassisPki.Certificate, chassisPki.CaCertificate)
            .AddSouthboundConnection(42421)
            .AddSouthboundConnection(42422, true)
            .AddSouthboundConnection(42423, true, IPAddress.Parse("203.0.113.2"));

    private OVNSouthboundControlTool CreateControlTool(int port, bool ssl)
    {
        OvsDbConnection dbConnection = ssl
            ? new OvsDbConnection("127.0.0.1", port, _clientPrivateKey, _clientCertificate, _clientCaCertificate)
            : new OvsDbConnection("127.0.0.1", port);
        
        return new OVNSouthboundControlTool(SystemEnvironment, dbConnection);
    }

    private async Task TestConnection(int port, bool ssl)
    {
        var controlTool = CreateControlTool(port, ssl);
        var sslEither = await controlTool.FindRecords<SouthboundGlobal>(
            OVNSouthboundTableNames.Global);
        var sslRecords = sslEither.ThrowIfLeft();
        sslRecords.Should().HaveCount(1);
    }
}
