using Dbosoft.OVN.OSCommands.OVN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbosoft.OVN.OSCommands;

public abstract class OVSControlToolBase : OVSTool
{
    private readonly OvsDbConnection _dbConnection;
    private readonly ISystemEnvironment _systemEnvironment;

    protected OVSControlToolBase(
        ISystemEnvironment systemEnvironment,
        OvsDbConnection dbConnection,
        OvsFile toolFile)
        : base(systemEnvironment, toolFile)
    {
        _systemEnvironment = systemEnvironment;
        _dbConnection = dbConnection;
    }

    protected override string BuildArguments(string command)
    {
        var sb = new StringBuilder();
        sb.Append($"--db=\"{_dbConnection.GetCommandString(_systemEnvironment.FileSystem, false)}\" ");

        if (_dbConnection.PrivateKeyFile is not null)
            sb.Append($"--private-key=\"{_systemEnvironment.FileSystem.ResolveOvsFilePath(_dbConnection.PrivateKeyFile, false)}\" ");
        
        if (_dbConnection.CertificateFile is not null)
            sb.Append($"--certificate=\"{_systemEnvironment.FileSystem.ResolveOvsFilePath(_dbConnection.CertificateFile, false)}\" ");
        
        if (_dbConnection.CaCertificateFile is not null)
            sb.Append($"--ca-cert=\"{_systemEnvironment.FileSystem.ResolveOvsFilePath(_dbConnection.CaCertificateFile, false)}\" ");
        
        sb.Append(base.BuildArguments(command));
        return sb.ToString();
    }
}
