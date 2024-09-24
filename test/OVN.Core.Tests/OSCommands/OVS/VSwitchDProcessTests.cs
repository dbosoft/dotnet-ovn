using System.Diagnostics;
using Dbosoft.OVN.OSCommands.OVS;
using Dbosoft.OVN.TestTools;
using Microsoft.Extensions.Logging;
using Moq;

namespace Dbosoft.OVN.Core.Tests.OSCommands.OVS;

public class VSwitchDProcessTests
{
    [Fact]
    public async Task VSwitchD_started_with_expected_arguments()
    {
        var processStartInfo = new ProcessStartInfo();
        var envMock = OvsMocks.SetupEnvForOvsTool(processStartInfo);
        var localSettings = new LocalOVSWithOVNSettings();
        localSettings.Logging.File.Level = OvsLogLevel.Warning;
        var loggerMock = new Mock<ILogger<VSwitchDProcess>>();

        await using var vSwitchDemon = new VSwitchDProcess(envMock.Object,
            new VSwitchDSettings(
                localSettings.SouthDBConnection,
                localSettings.Logging,
                false),
            false,
            loggerMock.Object);

        await vSwitchDemon.Start();
        Assert.Equal(
            // ReSharper disable once StringLiteralTypo
            @"""unix:/var/run/ovn/ovnsb_db.sock"" "
                + @"--unixctl=""/var/run/openvswitch/ovs-vswitchd.ctl"" "
                + @"--pidfile=""/var/run/openvswitch/ovs-vswitchd.pid"" "
                + @"--log-file=""/var/log/openvswitch/ovs-vswitchd.log"" "
                + @"--verbose=""file:warn""",
            processStartInfo.Arguments);
        
    }
}