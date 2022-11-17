using Dbosoft.OVN.OSCommands.OVN;
using LanguageExt;
using LanguageExt.Common;

namespace Dbosoft.OVN.OSCommands.OVS;

public class OVNAppControl : OVSTool, IAppControl
{
    private readonly OvsFile _controlFile;
    private readonly ISysEnvironment _sysEnv;

    public OVNAppControl(ISysEnvironment sysEnv, OvsFile controlFile) : base(sysEnv, OVNCommands.AppControl)
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

    protected override string BuildArguments(string command)
    {
        var controlFilePath = _sysEnv.FileSystem.ResolveOvsFilePath(_controlFile, false);

        return $"--target=\"{controlFilePath}\" {command}";
    }
}