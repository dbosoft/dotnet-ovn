using System.Data;
using System.Net;
using System.Text;
using Dbosoft.OVN.Model;
using LanguageExt;
using LanguageExt.Common;

using static LanguageExt.Prelude;

namespace Dbosoft.OVN.OSCommands.OVS;

public class OVSControlTool : OVSTool
{
    private readonly OvsDbConnection _dbConnection;
    private readonly bool _noWait;
    private readonly ISystemEnvironment _systemEnvironment;

    public OVSControlTool(
        ISystemEnvironment systemEnvironment,
        OvsDbConnection dbConnection,
        bool noWait = false)
        : base(systemEnvironment, OVSCommands.VSwitchControl)
    {
        _systemEnvironment = systemEnvironment;
        _dbConnection = dbConnection;
        _noWait = noWait;
    }

    protected override string BuildArguments(string command)
    {
        var sb = new StringBuilder();
        if (_noWait)
            sb.Append("--no-wait ");
        
        sb.Append($"--db=\"{_dbConnection.GetCommandString(_systemEnvironment.FileSystem, false)}\" ");
        sb.Append(base.BuildArguments(command));
        return sb.ToString();
    }

    public EitherAsync<Error, Unit> InitDb(CancellationToken cancellationToken = default)
    {
        return RunCommand("--no-wait init", true, cancellationToken).Map(_ => Unit.Default);
    }

    public EitherAsync<Error, Unit> ConfigureOVN(
        OvsDbConnection sbDBConnection,
        string chassisName,
        IPAddress? encapIp = null,
        string encapType = "geneve",
        CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();
        sb.Append(
            $"-- set open . external-ids:ovn-remote=\"{sbDBConnection.GetCommandString(_systemEnvironment.FileSystem, false)}\" ");
        sb.Append($"-- set open . external-ids:ovn-encap-type={encapType} ");
        sb.Append($"-- set open . external-ids:ovn-encap-ip={encapIp ?? IPAddress.Loopback} ");
        sb.Append($"-- set open . external-ids:system-id={chassisName} ");

        return RunCommand(sb.ToString(), true, cancellationToken).Map(_ => Unit.Default);
    }

    public EitherAsync<Error, OVSTableRecord> GetOVSTable(CancellationToken cancellationToken) =>
        from optionalRecord in GetRecord<OVSTableRecord>("open", ".", cancellationToken: cancellationToken)
        from ovsRecord in optionalRecord.ToEitherAsync(Error.New("The OVS configuration table does not exist."))
        select ovsRecord;

    public EitherAsync<Error, Unit> UpdateBridgeMapping(
        string bridgeMappings,
        CancellationToken cancellationToken) =>
        from ovsRecord in GetOVSTable(cancellationToken)
        let externalIds = ovsRecord.ExternalIds
            .Remove("ovn-bridge-mappings")
            .Add("ovn-bridge-mappings", bridgeMappings)
        from _ in UpdateRecord("open", ".",
            Map<string, IOVSField>(),
            Map<string, IOVSField>(("external_ids", OVSMap<string>.New(externalIds))),
            Seq<string>(),
            cancellationToken)
        select Unit.Default;
}