using System.ServiceProcess;
using System.Text;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Win32;

using static LanguageExt.Prelude;

namespace Dbosoft.OVN.Windows;

internal class WindowsServiceManager(
    string serviceName,
    ISystemEnvironment systemEnvironment)
    : IServiceManager
{
    public EitherAsync<Error, bool> ServiceExists() =>
        use(GetServiceController(),
            serviceController =>
                from actualServiceName in Eff(() => serviceController.ServiceName)
                select actualServiceName == serviceName)
            .IfFail(_ => false)
            .Run().ToEither().ToAsync();

    public EitherAsync<Error, string> GetServiceCommand() =>
        EffMaybe(() =>
        {
            using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{serviceName}");

            if (key is null)
                return Error.New($"Could not find service '{serviceName}'.");

            return key.GetValue("ImagePath") is string value
                ? FinSucc(value)
                : Error.New($"The service command of '{serviceName}' is invalid.");
        }).Run().ToEither().ToAsync();

    public EitherAsync<Error, Unit> CreateService(
        string displayName,
        string command,
        Seq<string> dependencies,
        CancellationToken cancellationToken) =>
        AffMaybe<Unit>(async () =>
        {
            var sb = new StringBuilder();
            sb.Append($"create {serviceName} ");
            sb.Append($"binPath=\"{command.Replace("\"", "\\\"")}\" ");
            sb.Append($"DisplayName=\"{displayName}\" ");
            sb.Append("start=auto ");
            if (!dependencies.IsEmpty)
                sb.Append($"depend=\"{string.Join("/", dependencies)}\" ");

            var scProcess = systemEnvironment.CreateProcess();
            scProcess.StartInfo.FileName = "sc.exe";
            scProcess.StartInfo.Arguments = sb.ToString();
            scProcess.StartInfo.RedirectStandardError = true;
            scProcess.StartInfo.RedirectStandardOutput = true;
            scProcess.Start();
            scProcess.BeginErrorReadLine();
            scProcess.BeginOutputReadLine();

            await scProcess.WaitForExit(cancellationToken);

            if (scProcess.ExitCode != 0)
                return Error.New($"Failed to create service {serviceName}");

            return unit;
        }).Run().AsTask().Map(r => r.ToEither()).ToAsync();

    public EitherAsync<Error, Unit> RemoveService(
        CancellationToken cancellationToken) =>
        from _ in EnsureServiceStopped(cancellationToken)
        from __ in AffMaybe<Unit>(async () =>
        {
            var sb = new StringBuilder();
            sb.Append($"delete {serviceName} ");

            var scProcess = systemEnvironment.CreateProcess();
            scProcess.StartInfo.FileName = "sc.exe";
            scProcess.StartInfo.Arguments = sb.ToString();
            scProcess.StartInfo.RedirectStandardError = true;
            scProcess.StartInfo.RedirectStandardOutput = true;
            scProcess.Start();
            scProcess.BeginErrorReadLine();
            scProcess.BeginOutputReadLine();

            await scProcess.WaitForExit(cancellationToken);

            if (scProcess.ExitCode != 0)
                return Error.New($"Failed to delete service {serviceName}");

            return Unit.Default;
        }).Run().AsTask().Map(r => r.ToEither()).ToAsync()
        select unit;

    public EitherAsync<Error, Unit> EnsureServiceStarted(
        CancellationToken cancellationToken) =>
        use(GetServiceController(),
            serviceController => Aff(async () => await Task.Factory.StartNew(() =>
            {
                if (serviceController.Status is ServiceControllerStatus.Running)
                    return Unit.Default;

                if (serviceController.Status != ServiceControllerStatus.StartPending)
                    serviceController.Start();

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        serviceController.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, 0, 1));
                    }
                    catch (System.ServiceProcess.TimeoutException)
                    {
                        // ignored
                    }

                    if (serviceController.Status == ServiceControllerStatus.Running)
                        return unit;
                }

                throw new OperationCanceledException("Failed to start service before operation was cancelled");
            }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current)))
            .Run().AsTask().Map(r => r.ToEither()).ToAsync();

    public EitherAsync<Error, Unit> EnsureServiceStopped(
        CancellationToken cancellationToken) =>
        use(GetServiceController(),
            serviceController => Aff(async () => await Task.Factory.StartNew(() =>
            {
                if (serviceController.Status is ServiceControllerStatus.Stopped)
                    return unit;

                if (serviceController.Status != ServiceControllerStatus.StopPending)
                    serviceController.Stop();

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        serviceController.WaitForStatus(ServiceControllerStatus.Stopped, new TimeSpan(0, 0, 1));
                    }
                    catch (System.ServiceProcess.TimeoutException)
                    {
                        // ignored
                    }

                    if (serviceController.Status == ServiceControllerStatus.Stopped)
                        return unit;
                }

                throw new OperationCanceledException("Failed to stop service before operation was cancelled");
            }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current)))
            .Run().AsTask().Map(r => r.ToEither()).ToAsync();

    public EitherAsync<Error, Unit> UpdateService(
        string command,
        CancellationToken cancellationToken) =>
        from _ in EffMaybe<Unit>(() =>
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                $@"SYSTEM\CurrentControlSet\Services\{serviceName}",
                true);

            if (key is null)
                return Error.New($"Could not find service '{serviceName}'.");

            key.SetValue("ImagePath", command);
            return unit;
        }).ToAff().Run().AsTask().Map(r => r.ToEither()).ToAsync()
        from _2 in EnsureServiceStopped(cancellationToken)
        from _3 in EnsureServiceStarted(cancellationToken)
        select unit;

    public EitherAsync<Error, Unit> SetRecoveryOptions(
        Option<TimeSpan> firstRestartDelay,
        Option<TimeSpan> secondRestartDelay,
        Option<TimeSpan> subsequentRestartDelay,
        Option<TimeSpan> resetDelay,
        CancellationToken cancellationToken) =>
        AffMaybe<Unit>(async () =>
        {
            // sc.exe expects just a slash / to indicate no action.
            // E.g. actions="/////" means no action on any failure.
            var actions = string.Join("/",
                ToRestartAction(firstRestartDelay).IfNone("/"),
                ToRestartAction(secondRestartDelay).IfNone("/"),
                ToRestartAction(subsequentRestartDelay).IfNone("/"));

            var resetSeconds = resetDelay
                .Map(d => d.Ticks / TimeSpan.TicksPerSecond)
                .Filter(s => s > 0)
                .IfNone(0);

            var sb = new StringBuilder();
            sb.Append($"failure {serviceName} ");
            sb.Append($"reset={resetSeconds} ");
            sb.Append($"actions=\"{actions}\"");

            var scProcess = systemEnvironment.CreateProcess();
            scProcess.StartInfo.FileName = "sc.exe";
            scProcess.StartInfo.Arguments = sb.ToString();
            scProcess.StartInfo.RedirectStandardError = true;
            scProcess.StartInfo.RedirectStandardOutput = true;
            scProcess.Start();
            scProcess.BeginErrorReadLine();
            scProcess.BeginOutputReadLine();

            await scProcess.WaitForExit(cancellationToken);

            if (scProcess.ExitCode != 0)
                return Error.New($"Failed to set recovery options for service {serviceName}");

            return unit;
        }).Run().AsTask().Map(r => r.ToEither()).ToAsync();

    private static Option<string> ToRestartAction(Option<TimeSpan> delay) =>
        delay.Map(ts => ts.Ticks / TimeSpan.TicksPerMillisecond)
            .Filter(ms => ms > 0)
            .Map(ms => $"restart/{ms}");

    private Eff<ServiceController> GetServiceController() =>
        Eff(() => new ServiceController(serviceName));
}
