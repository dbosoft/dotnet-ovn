using AwesomeAssertions;

namespace Dbosoft.OVNAgent.Tests;

public class ChassisPlanParserTests
{
    [Fact]
    public void ParseYaml_ValidPlan_ReturnsPlan()
    {
        const string yaml = """
                            name: test-chassis
                            tunnel_endpoints:
                            - ip_address: 203.0.113.2
                              encapsulation_type: geneve
                            bridge_mappings:
                              extern: br-extern
                            """;

        var plan = ChassisPlanParser.ParseYaml(yaml);

        plan.ChassisId.Should().Be("test-chassis");
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
