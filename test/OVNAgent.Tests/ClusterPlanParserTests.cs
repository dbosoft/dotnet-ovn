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
    }
}
