using System.Diagnostics;
using Dbosoft.OVN.OSCommands.OVN;
using Dbosoft.OVN.TestTools;
using Microsoft.Extensions.Logging;
using Moq;

namespace Dbosoft.OVN.Core.Tests.OSCommands.OVN;

public class OVNControllerProcessTests
{
    [Fact]
    public async Task OVNController_started_with_expected_arguments()
    {
        var processStartInfo = new ProcessStartInfo();
        var envMock = OvsMocks.SetupEnvForOvsTool(processStartInfo);
        var localSettings = new LocalOVSWithOVNSettings();
        var loggerMock = new Mock<ILogger<OVNControllerProcess>>();

        await using var ovnController = new OVNControllerProcess(envMock.Object,
            new OVNControllerSettings(localSettings.SouthDBConnection, false),
            loggerMock.Object);

        await ovnController.Start();
        // ReSharper disable once StringLiteralTypo
        Assert.Equal(@"--pidfile=""/var/run/ovn/ovn-controller.pid"" ""unix:/var/run/ovn/ovnsb_db.sock""", processStartInfo.Arguments);
        
    }
}