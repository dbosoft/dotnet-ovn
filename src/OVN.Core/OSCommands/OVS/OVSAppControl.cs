using LanguageExt;
using LanguageExt.Common;

namespace Dbosoft.OVN.OSCommands.OVS;

public class OVSAppControl : OVSTool, IAppControl
{
    private readonly OvsFile _controlFile;
    private readonly ISysEnvironment _sysEnv;

    public OVSAppControl(ISysEnvironment sysEnv, OvsFile controlFile) : base(sysEnv, OVSCommands.AppControl)
    {
        _sysEnv = sysEnv;
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

    public EitherAsync<Error, Unit> SetLogFileLevel(string level, CancellationToken cancellationToken = default)
    {
        return RunCommand($"vlog/set file:{level}", false, cancellationToken).Map(_ => Unit.Default);
    }

    protected override string BuildArguments(string command)
    {
        var controlFilePath = _sysEnv.FileSystem.ResolveOvsFilePath(_controlFile, false);

        return $"--target=\"{controlFilePath}\" {command}";
    }
}