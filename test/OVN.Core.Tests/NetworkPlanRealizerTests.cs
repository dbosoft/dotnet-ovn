using Dbosoft.OVN.Model;
using Dbosoft.OVN.Model.OVN;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;
using Moq;

namespace Dbosoft.OVN.Core.Tests;

public class NetworkPlanRealizerTests
{
    [Fact]
    public async Task ApplyPlan()
    {
        var mockTool = new Mock<IOVSDBTool>();
        var logger = new Mock<ILogger>();

        SetFindRecordsResult(mockTool, OVNTableNames.DHCPOptions, Array.Empty<DHCPOptions>());
        //SetFindRecordsResult(mockTool, OVNTableNames.DHCPOptions, 
        //    Array.Empty<DHCPOptions>(), OVSTableRecord.Columns.Keys);

        SetFindRecordsResult(mockTool, OVNTableNames.LogicalSwitch, Array.Empty<LogicalSwitch>());
        SetFindRecordsResult(mockTool, OVNTableNames.LogicalRouter,Array.Empty<LogicalRouter>());
        SetFindRecordsResult(mockTool, OVNTableNames.LogicalSwitchPort,Array.Empty<LogicalSwitchPort>());
        SetFindRecordsResult(mockTool, OVNTableNames.LogicalRouterPort,Array.Empty<LogicalRouterPort>());
        SetFindRecordsResult(mockTool, OVNTableNames.NATRules,Array.Empty<NATRule>());
        SetFindRecordsResult(mockTool, OVNTableNames.LogicalRouterStaticRoutes,Array.Empty<LogicalRouterStaticRoute>());
        
        var netplan = new NetworkPlan("id")
            .AddSwitch("test_switch");

        var realizer = new NetworkPlanRealizer(mockTool.Object, logger.Object);
        await realizer.ApplyNetworkPlan(netplan);
        
    }

    private static void SetFindRecordsResult<T>(
        Mock<IOVSDBTool> mock, 
        string tableName,
        IEnumerable<T> result, 
        Seq<string> columns = default) 
        where T : OVSTableRecord, new()
    {
        mock.Setup(x =>
                x.FindRecords<T>(tableName,
                    It.IsAny<Map<string, OVSQuery>>(),
                    columns.IsEmpty ? OVSEntityMetadata.Get(typeof(T)).Keys.ToSeq() : columns, 
                    CancellationToken.None))
            .Returns(EitherAsync<Error, Seq<T>>.Right(
                result.ToSeq()));
    }
}