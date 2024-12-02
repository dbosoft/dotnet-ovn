using Dbosoft.OVN.Logging;
using LanguageExt;
using LanguageExt.Common;

namespace Dbosoft.OVN.OSCommands.OVS;

public class OVSAppControl : OVSTool, IAppControl
{
    private readonly OvsFile _controlFile;
    private readonly ISystemEnvironment _systemEnvironment;

    public OVSAppControl(
        ISystemEnvironment systemEnvironment,
        OvsFile controlFile)
        : base(systemEnvironment, OVSCommands.AppControl)
    {
        _systemEnvironment = systemEnvironment;
        _controlFile = controlFile;
    }

    public EitherAsync<Error, Unit> StopApp(CancellationToken cancellationToken = default)
    {
        return RunCommand("exit", false, cancellationToken).Map(_ => Unit.Default);
    }

    public EitherAsync<Error, string> GetVersion(CancellationToken cancellationToken = default)
    {
        return RunCommandWithResponse("version", cancellationToken);
    }

    public EitherAsync<Error, Unit> SetLogging(
        OvsLoggingSettings loggingSettings,
        CancellationToken cancellationToken = default)
    {
        var logLevel = loggingSettings.File.Level.ToOvsValue();
        return RunCommand($"vlog/set file:{logLevel}", false, cancellationToken).Map(_ => Unit.Default);
    }

    protected override string BuildArguments(string command)
    {
        var controlFilePath = _systemEnvironment.FileSystem.ResolveOvsFilePath(_controlFile, false);

        return $"--target=\"{controlFilePath}\" {command}";
    }
}