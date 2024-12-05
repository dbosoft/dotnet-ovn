using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Dbosoft.OVN.OSCommands;
using Moq;
using DataReceivedEventArgs = Dbosoft.OVN.OSCommands.DataReceivedEventArgs;

namespace Dbosoft.OVN.TestTools;

public static class OvsMocks
{
    public static Mock<ISystemEnvironment> SetupEnvForOvsTool(
        ProcessStartInfo startInfo, string outputString = "", string errorString = "", int exitCode = 0)
    {
        var res = SetupEnvForOvsToolWithProcess(startInfo, outputString, errorString, exitCode);
        return res.Item1;
    }
    
    public static (Mock<ISystemEnvironment> SysEnv, Mock<IProcess> Process) SetupEnvForOvsToolWithProcess(
        ProcessStartInfo startInfo, string outputString = "", string errorString = "", int exitCode = 0)
    {
        var mockEnv = new Mock<ISystemEnvironment>();
        var processMock = new Mock<IProcess>();

        var outputStreamReader = new StreamReader(
            new MemoryStream(Encoding.UTF8.GetBytes(outputString)));
        
        var errorStreamReader = new StreamReader(
            new MemoryStream(Encoding.UTF8.GetBytes(errorString)));
        
        mockEnv.Setup(x => x.CreateProcess(0)).Returns(processMock.Object);
        mockEnv.Setup(x => x.FileSystem).Returns(
            new DefaultFileSystem(OSPlatform.Create("LINUX")));
        processMock.Setup(x => x.StartInfo).Returns(startInfo);
        processMock.Setup(x => x.WaitForExit()).Callback(() =>
        {
            if(!string.IsNullOrWhiteSpace(outputString))
                processMock.Raise(p => p.OutputDataReceived += null, new DataReceivedEventArgs(outputString));
            if(!string.IsNullOrWhiteSpace(errorString))
                processMock.Raise(p => p.ErrorDataReceived += null, new DataReceivedEventArgs(errorString));
            processMock.Raise(p => p.OutputDataReceived += null, new DataReceivedEventArgs(null));
            processMock.Raise(p => p.ErrorDataReceived += null, new DataReceivedEventArgs(null));

        });
       
        processMock.Setup(x => x.HasExited).Returns(true);
        processMock.Setup(x => x.ExitCode).Returns(exitCode);
        return (mockEnv, processMock);
    }
}