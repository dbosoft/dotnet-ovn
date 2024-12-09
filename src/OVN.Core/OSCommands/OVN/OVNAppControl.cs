using Dbosoft.OVN.OSCommands.OVN;
using LanguageExt;
using LanguageExt.Common;

namespace Dbosoft.OVN.OSCommands.OVS;

public class OVNAppControl : OVSTool, IAppControl
{
    private readonly OvsFile _controlFile;
    private readonly ISystemEnvironment _systemEnvironment;

    public OVNAppControl(
        ISystemEnvironment systemEnvironment,
        OvsFile controlFile)
        : base(systemEnvironment, OVNCommands.AppControl)
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

    protected override string BuildArguments(string command)
    {
        var controlFilePath = _systemEnvironment.FileSystem.ResolveOvsFilePath(_controlFile, false);

        return $"--target=\"{controlFilePath}\" {command}";
    }
}