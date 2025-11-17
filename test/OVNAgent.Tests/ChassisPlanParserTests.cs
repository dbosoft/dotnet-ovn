using AwesomeAssertions;

namespace Dbosoft.OVNAgent.Tests;

public class ChassisPlanParserTests
{
    [Fact]
    public void ParseYaml_ValidPlan_ReturnsPlan()
    {
        const string yaml = """
                            name: test-chassis
                            southbound_connection:
                              ip_address: 203.0.113.1
                              port: 42421
                              ssl: true
                            ssl:
                              private_key: test-private-key
                              certificate: test-certificate
                              ca_certificate: test-ca-certificate
                            tunnel_endpoints:
                            - ip_address: 203.0.113.2
                              encapsulation_type: geneve
                            bridge_mappings:
                              extern: br-extern
                            """;

        var plan = ChassisPlanParser.ParseYaml(yaml);

        plan.ChassisId.Should().Be("test-chassis");

        plan.SouthboundDatabase.Address.Should().Be("203.0.113.1");
        plan.SouthboundDatabase.Port.Should().Be(42421);
        plan.SouthboundDatabase.Ssl.Should().BeTrue();
        plan.SouthboundDatabase.PipeFile.Should().BeNull();

        plan.PlannedSwitchSsl.Should().NotBeNull();
        plan.PlannedSwitchSsl.PrivateKey.Should().Be("test-private-key");
        plan.PlannedSwitchSsl.Certificate.Should().Be("test-certificate");
        plan.PlannedSwitchSsl.CaCertificate.Should().Be("test-ca-certificate");

        plan.TunnelEndpoints.Should().HaveCount(1);
        plan.TunnelEndpoints.Should().SatisfyRespectively(
            plannedEndpoint =>
            {
                plannedEndpoint.EncapsulationType.Should().Be("geneve");
                plannedEndpoint.IpAddress.ToString().Should().Be("203.0.113.2");
            });

        plan.BridgeMappings.Should().HaveCount(1);
        plan.BridgeMappings.ToDictionary()
            .Should().ContainKey("extern")
            .WhoseValue.Should().Be("br-extern");
    }
}
