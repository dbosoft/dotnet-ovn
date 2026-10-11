using System.Diagnostics;
using Dbosoft.OVN.OSCommands.OVS;
using Dbosoft.OVN.TestTools;

namespace Dbosoft.OVN.Core.Tests.OSCommands.OVS;

public class OVSControlToolTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InitDb_does_not_wait_for_vswitchd(bool noWait)
    {
        var processStartInfo = new ProcessStartInfo();
        var envMock = OvsMocks.SetupEnvForOvsTool(processStartInfo);
        var connection = new OvsDbConnection(new OvsFile("/var/run/openvswitch", "db.sock"));

        var ovsControl = new OVSControlTool(envMock.Object, connection, noWait);
        var result = await ovsControl.InitDb();

        Assert.True(result.IsRight);
        var arguments = processStartInfo.Arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(1, arguments.Count(a => a == "--no-wait"));
        Assert.Equal("init", arguments.Last());
    }
}
