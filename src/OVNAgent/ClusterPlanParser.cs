using Dbosoft.OVN;

namespace Dbosoft.OVNAgent;

public static class ClusterPlanParser
{
    public static ClusterPlan ParseYaml(IDictionary<object, object> dictionary)
    {
        var clusterPlan = new ClusterPlan();

        if (dictionary.ContainsKey("chassis_groups")
            && dictionary["chassis_groups"] is IList<object> chassisGroups)
        {
            clusterPlan = ParseChassisGroups(clusterPlan, chassisGroups);
        }

        return clusterPlan;
    }

    private static ClusterPlan ParseChassisGroups(
        ClusterPlan networkPlan,
        IEnumerable<object> chassisGroups)
    {
        foreach (var chassisGroupObj in chassisGroups)
        {
            if (chassisGroupObj is not IDictionary<object, object> chassisGroup)
                continue;

            networkPlan = ParseChassisGroup(networkPlan, chassisGroup);
        }

        return networkPlan;
    }

    private static ClusterPlan ParseChassisGroup(
        ClusterPlan networkPlan,
        IDictionary<object, object> chassisGroupValues)
    {
        if (!chassisGroupValues.ContainsKey("name") || chassisGroupValues["name"] is not string chassisGroupName)
            throw new InvalidDataException("chassis group name is required.");

        networkPlan = networkPlan.AddChassisGroup(chassisGroupName);

        if (!chassisGroupValues.ContainsKey("chassis") || chassisGroupValues["chassis"] is not IList<object> chassisList)
            return networkPlan;

        foreach (var chassisObj in chassisList)
        {
            if (chassisObj is IDictionary<object, object> chassisValues)
                networkPlan = ParseChassis(networkPlan, chassisValues, chassisGroupName);
        }

        return networkPlan;
    }

    private static ClusterPlan ParseChassis(
        ClusterPlan clusterPlan,
        IDictionary<object, object> chassisValues,
        string chassisGroupName)
    {
        if (!chassisValues.ContainsKey("name") || chassisValues["name"] is not string chassisName)
            throw new InvalidDataException("chassis name is required.");

        short priority = 0;
        if (chassisValues.ContainsKey("priority") && chassisValues["priority"] is int prio)
        {
            priority = (short)prio;
        }
        return clusterPlan.AddChassis(chassisGroupName, chassisName, priority);
    }

}
