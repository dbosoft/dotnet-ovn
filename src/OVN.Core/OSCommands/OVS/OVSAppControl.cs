using LanguageExt;
using LanguageExt.Common;

namespace Dbosoft.OVN.OSCommands.OVS;

public class OVSAppControl : OVSTool
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
        return RunCommand("exit", cancellationToken).Map(_ => Unit.Default);
    }

    public EitherAsync<Error, string> GetVersion(CancellationToken cancellationToken = default)
    {
        return RunCommand("version", cancellationToken);
    }

    protected override string BuildArguments(string command)
    {
        var controlFilePath = _sysEnv.FileSystem.ResolveOvsFilePath(_controlFile, false);

        return $"--target=\"{controlFilePath}\" {command}";
    }
}