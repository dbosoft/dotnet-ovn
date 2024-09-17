using System.Diagnostics;
using Dbosoft.OVN.OSCommands.OVN;
using Dbosoft.OVN.TestTools;
using Microsoft.Extensions.Logging;
using Moq;

// ReSharper disable StringLiteralTypo

namespace Dbosoft.OVN.Core.Tests.OSCommands.OVN;

public class NorthDProcessTests
{
    [Fact]
    public async Task NorthD_started_with_expected_arguments()
    {
        var processStartInfo = new ProcessStartInfo();
        var envMock = OvsMocks.SetupEnvForOvsTool(processStartInfo);
        var localSettings = new LocalOVSWithOVNSettings();
        var loggerMock = new Mock<ILogger<NorthDProcess>>();

        await using var northDProcess = new NorthDProcess(envMock.Object,
            new NorthDSettings(localSettings.NorthDBConnection, localSettings.SouthDBConnection, "warn", false),
            loggerMock.Object);

        await northDProcess.Start();
         Assert.Equal(
             @"--ovnnb-db=""unix:/var/run/ovn/ovnnb_db.sock"" "
                + @"--ovnsb-db=""unix:/var/run/ovn/ovnsb_db.sock"" "
                + @"--unixctl=""/var/run/ovn/ovn-northd.ctl"" "
                + @"--pidfile=""/var/run/ovn/ovn-northd.pid"" "
                + @"--log-file=""/var/log/ovn/ovn-northd.log"" "
                + @"--verbose=""file:warn""",

             processStartInfo.Arguments);
        
    }
}