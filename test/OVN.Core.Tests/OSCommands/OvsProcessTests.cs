using System.Diagnostics;
using Dbosoft.OVN.OSCommands;
using Dbosoft.OVN.TestTools;
using LanguageExt;
using Moq;

namespace Dbosoft.OVN.Core.Tests.OSCommands;

public class OvsProcessTests
{

    [Fact]
    public void Process_is_Started_with_required_settings()
    {
        var processStartInfo = new ProcessStartInfo();
        var mockEnv = OvsMocks.SetupEnvForOvsTool(processStartInfo);
        
        using var ovsProcess = new OVSProcess(
            mockEnv.Object,
            new OvsFile("bin", "test", true),
            "testarg1 testarg2");

        ovsProcess.Start();
        
        Assert.Equal("/bin/test", processStartInfo.FileName);
        Assert.Equal("testarg1 testarg2", processStartInfo.Arguments);
    }
    
    [Fact]
    public void Process_is_Started_with_message_handlers()
    {
        var processStartInfo = new ProcessStartInfo();
        var (mockEnv, mockProcess) = OvsMocks.SetupEnvForOvsToolWithProcess(processStartInfo);
        mockProcess.Setup(x=>x.BeginErrorReadLine()).Verifiable();
        mockProcess.Setup(x=>x.BeginOutputReadLine()).Verifiable();
        
        using var ovsProcess = new OVSProcess(
            mockEnv.Object,
            new OvsFile("bin", "test", true),
            "testarg1 testarg2");
        
        ovsProcess.AddMessageHandler((_) =>{} );

        ovsProcess.Start().Match(
            _ =>
            {
                mockProcess.Verify();

            },
            f => throw f
        );
 
            
    }

    [Fact]
    public async Task Process_will_be_killed()
    {
        var processStartInfo = new ProcessStartInfo();
        var (mockEnv, mockProcess) = OvsMocks.SetupEnvForOvsToolWithProcess(processStartInfo);
        mockProcess.Setup(x=>x.Kill()).Verifiable();
        mockProcess.Setup(x => x.WaitForExit(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        using var ovsProcess = new OVSProcess(
            mockEnv.Object,
            new OvsFile("bin", "test", true),
            "testarg1 testarg2");
        // ReSharper disable once AccessToDisposedClosure
        await ovsProcess.Start().Map(_ => Unit.Default).ToAsync()
            .Bind(_ => ovsProcess.KillAsync())
            .Match(
                _ => { mockProcess.Verify(); },
                f => throw f);
    }

    [Fact]
    public async Task WaitForExit_throws_without_process()
    {
        var mockEnv = new Mock<ISystemEnvironment>();
        using var ovsProcess = new OVSProcess(
            mockEnv.Object,
            new OvsFile("bin", "test", true));

        await Assert.ThrowsAsync<IOException>(async () =>
        {
            var t = await ovsProcess.WaitForExit(false, CancellationToken.None);
            _ = t().IfFail(f => throw f);
        });
    }
    
    [Fact]
    public async Task WaitForExit_throws_if_process_not_exited()
    {
        var processStartInfo = new ProcessStartInfo();
        var (mockEnv, mockProcess) = OvsMocks.SetupEnvForOvsToolWithProcess(processStartInfo);
        mockProcess.Setup(x => x.HasExited).Returns(false);
        
        using var ovsProcess = new OVSProcess(
            mockEnv.Object,
            new OvsFile("bin", "test", true));
        
        ovsProcess.Start();
        
        var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAsync<TimeoutException>(async () =>
        {
            var t = await ovsProcess.WaitForExit(false, cts.Token);
            _ = t().IfFail(f => throw f);
        });
    }
}