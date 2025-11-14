using System.Net;
using AwesomeAssertions;
using Dbosoft.OVN.Model.OVN;
using Dbosoft.OVN.OSCommands.OVN;
using Xunit.Abstractions;

namespace Dbosoft.OVN.Core.IntegrationTests;

public class ClusterPlanSouthboundRealizerTests(ITestOutputHelper testOutputHelper)
    : OvnSouthboundControlToolTestBase(testOutputHelper)
{
    [Fact]
    public async Task ApplyClusterPlan_NewPlan_IsSuccessful()
    {
        await ApplyClusterPlan(CreateClusterPlan());

        await VerifyDatabase();

        // Verify that the OVN Southbound database is listening on the expected port
        var networkControlTool = new OVNSouthboundControlTool(
            SystemEnvironment,
            new OvsDbConnection("127.0.0.1", 42421));
        var either = await networkControlTool.FindRecords<SouthboundConnection>(
            OVNSouthboundTableNames.Connection);

        var records = either.ThrowIfLeft();
        records.Should().HaveCount(3);

        var sslControlTool = CreateControlTool(42422, true);
        var sslEither = await sslControlTool.FindRecords<SouthboundConnection>(
            OVNSouthboundTableNames.Connection);
        var sslRecords = sslEither.ThrowIfLeft();
        sslRecords.Should().HaveCount(3);
    }

    [Fact]
    public async Task ApplyClusterPlan_UpdatedPlan_IsSuccessful()
    {
        await ApplyClusterPlan(CreateClusterPlan());

        var updatedPlan = new ClusterPlan()
            .AddSouthboundConnection(52421)
            .AddSouthboundConnection(52422, true);

        await ApplyClusterPlan(updatedPlan);

        await VerifyDatabase();

        // Verify that the OVN Southbound database is listening on the expected port
        var networkControlTool = new OVNSouthboundControlTool(
            SystemEnvironment,
            new OvsDbConnection("127.0.0.1", 52421));
        var either = await networkControlTool.GetRecord<SouthboundGlobal>(
            OVNSouthboundTableNames.Global,
            ".");

        var record = either.ThrowIfLeft();
        record.IsSome.Should().BeTrue();

        var sslControlTool = CreateControlTool(52422, true);
        var sslEither = await sslControlTool.FindRecords<SouthboundConnection>(
            OVNSouthboundTableNames.Connection);
        var sslRecords = sslEither.ThrowIfLeft();
        sslRecords.Should().HaveCount(2);
    }

    private async Task ApplyClusterPlan(ClusterPlan clusterPlan)
    {
        var realizer = new ClusterPlanSouthboundRealizer(SystemEnvironment, ControlTool);

        var either = await realizer.ApplyClusterPlan(clusterPlan);
        either.ThrowIfLeft();
    }

    private ClusterPlan CreateClusterPlan() =>
        new ClusterPlan()
            .SetSouthboundSsl(TestData.PrivateKey, TestData.Certificate, TestData.CaCertificate)
            .AddSouthboundConnection(42421)
            .AddSouthboundConnection(42422, true)
            .AddSouthboundConnection(42423, true, IPAddress.Parse("203.0.113.2"));

    private OVNSouthboundControlTool CreateControlTool(int port, bool ssl)
    {
        OvsDbConnection dbConnection = ssl
            ? new OvsDbConnection(
                "127.0.0.1",
                port,
                OvsCertificateFileHelper.ComputePrivateKeyPath(TestData.PrivateKey),
                OvsCertificateFileHelper.ComputeCertificatePath(TestData.Certificate),
                OvsCertificateFileHelper.ComputeCaCertificatePath(TestData.CaCertificate))
            : new OvsDbConnection("127.0.0.1", port);
        
        return new OVNSouthboundControlTool(SystemEnvironment, dbConnection);
    }
}
