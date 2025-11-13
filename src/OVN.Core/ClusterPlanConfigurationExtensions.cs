using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Dbosoft.OVN.Model.OVN;

namespace Dbosoft.OVN;

public static class ClusterPlanConfigurationExtensions
{
    public static ClusterPlan AddChassisGroup(
        this ClusterPlan plan,
        string chassisGroupName)
    {
        return plan with
        {
            PlannedChassisGroups = plan.PlannedChassisGroups.Add(chassisGroupName, new PlannedChassisGroup
            {
                Name = chassisGroupName,
            }),
        };
    }

    public static ClusterPlan AddChassis(
        this ClusterPlan plan,
        string chassisGroupName,
        string chassisName,
        short priority = 0)
    {
        return plan with
        {
            PlannedChassis = plan.PlannedChassis.Add(chassisName, new PlannedChassis(chassisGroupName)
            {
                Name = chassisName,
                Priority = priority
            }),
        };
    }

    public static ClusterPlan AddSouthboundConnection(
        this ClusterPlan plan,
        int port = 6642,
        bool ssl = false,
        IPAddress? ipAddress = null)
    {
        var protocol = ssl ? "pssl" : "ptcp";
        var target = ipAddress is null ? $"{protocol}:{port}" : $"{protocol}:{port}:{ipAddress}";
        return plan with
        {
            PlannedSouthboundConnections = plan.PlannedSouthboundConnections.Add(
                target,
                new PlannedSouthboundConnection { Target = target }),
        };
    }

    public static ClusterPlan SetSouthboundSsl(
        this ClusterPlan plan,
        string privateKey,
        string certificate,
        string caCertificate)
    {
        return plan with
        {
            PlannedSouthboundSsl = new PlannedSouthboundSsl
            {
                PrivateKey = privateKey,
                Certificate = certificate,
                CaCertificate = caCertificate,
                SslProtocols = "TLSv1.2",
            }
        };
    }
}
