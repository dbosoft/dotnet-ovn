using AwesomeAssertions;

namespace Dbosoft.OVNAgent.Tests;

public class ClusterPlanParserTests
{
    [Fact]
    public void ParseYaml_ValidPlan_ReturnsPlan()
    {
        const string yaml = """
                            chassis_groups:
                            - name: test-cluster
                              chassis:
                              - name: test-chassis-1
                                priority: 10
                            southbound_endpoints:
                            - port: 42421
                            - port: 42422
                              ssl: true
                              ip_address: 203.0.113.2
                            southbound_ssl:
                              private_key: test-private-key
                              certificate: test-certificate
                              ca_certificate: test-ca-certificate
                            """;

        var plan = ClusterPlanParser.ParseYaml(yaml);

        plan.PlannedChassisGroups.Should().HaveCount(1);
        var plannedChassisGroup = plan.PlannedChassisGroups.ToDictionary()
            .Should().ContainKey("test-cluster").WhoseValue;
        plannedChassisGroup.Name.Should().Be("test-cluster");

        plan.PlannedChassis.Should().HaveCount(1);
        var plannedChassis = plan.PlannedChassis.ToDictionary()
            .Should().ContainKey("test-chassis-1").WhoseValue;

        plannedChassis.Name.Should().Be("test-chassis-1");
        plannedChassis.ChassisGroupName.Should().Be("test-cluster");
        plannedChassis.Priority.Should().Be(10);

        plan.PlannedSouthboundConnections.Should().HaveCount(2);
        plan.PlannedSouthboundConnections.ToDictionary()
            .Should().ContainKey("ptcp:42421")
            .WhoseValue.Target.Should().Be("ptcp:42421");
        plan.PlannedSouthboundConnections.ToDictionary()
            .Should().ContainKey("pssl:42422:203.0.113.2")
            .WhoseValue.Target.Should().Be("pssl:42422:203.0.113.2");

        plan.PlannedSouthboundSsl.Should().NotBeNull();
        plan.PlannedSouthboundSsl.PrivateKey.Should().Be("test-private-key");
        plan.PlannedSouthboundSsl.Certificate.Should().Be("test-certificate");
        plan.PlannedSouthboundSsl.CaCertificate.Should().Be("test-ca-certificate");
    }
}
