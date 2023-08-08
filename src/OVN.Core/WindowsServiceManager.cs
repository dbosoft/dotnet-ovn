using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.ServiceProcess;
using System.Text;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Win32;

namespace Dbosoft.OVN;

[SupportedOSPlatform("windows")]
internal class WindowsServiceManager : IServiceManager
{
    private readonly string _serviceName;
    private readonly ISysEnvironment _sysEnv;

    public WindowsServiceManager(string serviceName, ISysEnvironment sysEnv)
    {
        _serviceName = serviceName;
        _sysEnv = sysEnv;
    }

    public EitherAsync<Error, bool> ServiceExists()
    {
        return 
            GetServiceController()
                .Match(l =>
                {
                    return Prelude.Try(
                            () => l.ServiceName == _serviceName)
                        .Match(Fail: _ => false, Succ: _ => true);
                }, l => false);
           
    }

    public EitherAsync<Error, string> GetServiceCommand()
    {
        return  Prelude.Try<Either<Error,string>>(() =>
            {
                using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{_serviceName}");
                // ReSharper disable once ConvertIfStatementToReturnStatement
                // if required, otherwise would result in Error response
                if(key?.GetValue("ImagePath") is not string value)
                    return Error.New($"Could not find service {_serviceName}");
                return value;
            }).Map(r => r.ToAsync())
            .ToEitherAsync().Flatten();
    }

    public EitherAsync<Error, Unit> CreateService(string displayName, string command, CancellationToken cancellationToken)
    {
        return Prelude.TryAsync<Either<Error, Unit>>(async () =>
            {
                var sb = new StringBuilder();
                sb.Append((string?)$"create {_serviceName} ");
                sb.Append((string?)$"binPath=\"{command.Replace("\"", "\\\"")}\" ");
                sb.Append((string?)$"DisplayName=\"{displayName}\" ");
                sb.Append($"start=auto ");

                var scProcess = _sysEnv.CreateProcess();
                scProcess.StartInfo.FileName = "sc.exe";
                scProcess.StartInfo.Arguments = sb.ToString();
                scProcess.StartInfo.RedirectStandardError = true;
                scProcess.StartInfo.RedirectStandardOutput = true;
                scProcess.Start();
                scProcess.BeginErrorReadLine();
                scProcess.BeginOutputReadLine();
                
                await scProcess.WaitForExit(cancellationToken);

                if (scProcess.ExitCode != 0)
                    return Error.New($"Failed to create service {_serviceName}");

                return Unit.Default;
            }).Map(r => r.ToAsync())
            .ToEither().Flatten();
    }

    public EitherAsync<Error, Unit> RemoveService(CancellationToken cancellationToken)
    {
        return
            EnsureServiceStopped(cancellationToken)
                .Bind(_ => 
                    Prelude.TryAsync<Either<Error, Unit>>(async () =>
                        {
                            var sb = new StringBuilder();
                            sb.Append((string?)$"delete {_serviceName} ");
                
                            var scProcess = _sysEnv.CreateProcess();
                            scProcess.StartInfo.FileName = "sc.exe";
                            scProcess.StartInfo.Arguments = sb.ToString();
                            scProcess.StartInfo.RedirectStandardError = true;
                            scProcess.StartInfo.RedirectStandardOutput = true;
                            scProcess.Start();
                            scProcess.BeginErrorReadLine();
                            scProcess.BeginOutputReadLine();
                            
                            await scProcess.WaitForExit(cancellationToken);

                            if (scProcess.ExitCode != 0)
                                return Error.New($"Failed to delete service {_serviceName}");

                            return Unit.Default;
                        }).Map(r => r.ToAsync())
                        .ToEither().Flatten());
    }

    public EitherAsync<Error, Unit> EnsureServiceStarted(CancellationToken cancellationToken)
    {
        return Prelude.Try(() =>
        {
            return GetServiceController().Map(c =>
            {
                if (c.Status is ServiceControllerStatus.Running)
                    return Unit.Default;

                if(c.Status != ServiceControllerStatus.StartPending)
                    c.Start();
                
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        c.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, 0, 1));
                    }
                    catch (System.ServiceProcess.TimeoutException)
                    {
                        // ignored
                    }

                    if (c.Status == ServiceControllerStatus.Running)
                        return Unit.Default;
                }

                throw new OperationCanceledException("Failed to start service before operation was cancelled");

            });
        }).ToEitherAsync().Flatten();
    }
    
    public EitherAsync<Error, Unit> EnsureServiceStopped(CancellationToken cancellationToken)
    {
        return Prelude.Try(() =>
        {
            return GetServiceController().Map(c =>
            {
                if (c.Status is ServiceControllerStatus.Stopped)
                    return Unit.Default;

                if(c.Status != ServiceControllerStatus.StopPending)
                    c.Stop();
                
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        c.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, 0, 1));
                    }
                    catch (System.ServiceProcess.TimeoutException)
                    {
                        // ignored
                    }                   
                    
                    if (c.Status == ServiceControllerStatus.Stopped)
                        return Unit.Default;
                }

                throw new OperationCanceledException("Failed to stop service before operation was cancelled");

            });
        }).ToEitherAsync().Flatten();
    }


    public EitherAsync<Error, Unit> UpdateService(string command, CancellationToken cancellationToken)
    {
        return Prelude.Try<Either<Error, Unit>>(() =>
            {
                using var key = 
                    Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{_serviceName}",true);
                if (key == null)
                    return Error.New("Could not find service");

                key.SetValue("ImagePath", command);
                return Unit.Default;
            }).Map(r => r.ToAsync())
            .ToEitherAsync().Flatten()
            .Bind(_ => EnsureServiceStopped(cancellationToken))
            .Bind(_ => EnsureServiceStarted(cancellationToken));
    }

    public EitherAsync<Error, ServiceController> GetServiceController()
    {
        return new ServiceController(_serviceName);
    }
}