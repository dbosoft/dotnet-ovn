using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Linq;
using System.Management;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LanguageExt;
using LanguageExt.Common;

using static LanguageExt.Prelude;

namespace Dbosoft.OVN;

/// <inheritdoc cref="IHyperVOvsPortManager"/>
[SupportedOSPlatform("windows")]
public sealed partial class HyperVOvsPortManager(
    TimeSpan timeOut,
    TimeSpan pollingInterval)
    : IHyperVOvsPortManager
{
    public HyperVOvsPortManager() : this(TimeSpan.FromMinutes(1), TimeSpan.FromMilliseconds(100))
    {
    }

    [GeneratedRegex("^[a-zA-Z0-9-_]+$")]
    private static partial Regex PortNameRegex();
    private const string Scope = @"root\virtualization\v2";

    private bool _disposed;
    private readonly Lazy<ManagementObject> _vmms = new(
        () =>
        {
            using var @class = new ManagementClass(
                new ManagementScope(Scope),
                new ManagementPath("Msvm_VirtualSystemManagementService"),
                null);
            using var instances = @class.GetInstances();
            return instances.Cast<ManagementObject>().Single();
        },
        LazyThreadSafetyMode.ExecutionAndPublication);

    /// <inheritdoc/>
    public EitherAsync<Error, Option<string>> GetPortName(string adapterId) =>
        from _ in guard(IsValidAdapterId(adapterId),
                Error.New($"The Hyper-V network adapter ID '{adapterId}' is invalid."))
            .ToEitherAsync()
        from adapterInfo in GetAdapterInfo(adapterId)
        let portName = adapterInfo.Map(i => i.ElementName)
        select portName;

    /// <inheritdoc/>
    public EitherAsync<Error, Seq<string>> GetAdapterIds(string portName) =>
        from _ in guard(IsValidPortName(portName),
                Error.New($"The OVS port name '{portName}' is invalid."))
            .ToEitherAsync()
        from instanceIds in TryAsync(Task.Run(() =>
            {
                using var searcher = new ManagementObjectSearcher(
                    new ManagementScope(Scope),
                    new ObjectQuery("SELECT InstanceID "
                                    + "FROM Msvm_EthernetPortAllocationSettingData "
                                    + $"WHERE ElementName = '{portName}'"));

                using var collection = searcher.Get();
                var results = collection.Cast<ManagementBaseObject>().ToList();
                try
                {
                    // Invoke ToList() to force eager evaluation of the LINQ query
                    return results.Map(r => (string)r["InstanceID"]).ToList().ToSeq();
                }
                finally
                {
                    DisposeAll(results);
                }
            }))
            .ToEither(e => Error.New($"Could not get adapter ID for OVS port name '{portName}'.",e))
        from adapterIds in instanceIds.Map(ExtractAdapterId).SequenceSerial()
        select adapterIds;

    /// <inheritdoc/>
    public EitherAsync<Error, Unit> SetPortName(
        string adapterId,
        string portName) =>
        from adapterInfo in GetAdapterInfo(adapterId)
        from validAdapterInfo in adapterInfo.ToEitherAsync(
            Error.New($"The Hyper-V network adapter '{adapterId}' does not exist."))
        from _2 in guard(IsValidPortName(portName),
            Error.New($"The OVS port name '{portName}' is invalid."))
        from jobPath in TryAsync(Task.Run(() =>
        {
            ManagementObject? adapterData = null;
            ManagementBaseObject? parameters = null;
            ManagementBaseObject? result = null;

            try
            {
                adapterData = new ManagementObject(
                    new ManagementScope(Scope),
                    new ManagementPath(validAdapterInfo.Path),
                    null);
                adapterData.Get();
                adapterData.SetPropertyValue("ElementName", portName);

                parameters = _vmms.Value.GetMethodParameters("ModifyResourceSettings");
                parameters["ResourceSettings"] = new[] { adapterData.GetText(TextFormat.WmiDtd20) };
                result = _vmms.Value.InvokeMethod("ModifyResourceSettings", parameters, null);

                var returnValue = (uint)result["ReturnValue"];
                return returnValue switch
                {
                    0 => Option<string>.None,
                    4096 => Some((string)result["Job"]),
                    _ => throw Error.New($"ModifyResourceSettings failed with result '{ConvertReturnValue(returnValue)}'"),
                };
            }
            finally
            {
                adapterData?.Dispose();
                parameters?.Dispose();
                result?.Dispose();
            }
        })).ToEither(e => Error.New($"Could not set OVS port name for adapter '{adapterId}'.", e))
        from _4 in jobPath.Map(WaitForJob).SequenceSerial()
        from reportedPortName in GetPortName(adapterId)
        from _5 in guard(reportedPortName == portName,
            Error.New($"The OVS port name was not properly set for the adapter '{adapterId}'"))
            .ToEitherAsync()
        select unit;

    private static EitherAsync<Error, Option<(string Path, string ElementName)>> GetAdapterInfo(
        string adapterId) =>
        from _ in guard(IsValidAdapterId(adapterId),
                Error.New($"The Hyper-V network adapter ID '{adapterId}' is invalid."))
            .ToEitherAsync()
        from portName in TryAsync(Task.Run(() =>
        {
            using var searcher = new ManagementObjectSearcher(
                new ManagementScope(Scope),
                new ObjectQuery("SELECT ElementName, __RELPATH "
                                + "FROM Msvm_EthernetPortAllocationSettingData "
                                + $"WHERE InstanceID LIKE '{adapterId.Replace(@"\", @"\\")}%'"));

            using var collection = searcher.Get();
            var results = collection.Cast<ManagementBaseObject>().ToList();
            try
            {
                if (results.Count == 0)
                    return Option<(string Path, string ElementName)>.None;

                var result = results.Single();
                return ((string)result["__RELPATH"], (string)result["ElementName"]);
            }
            finally
            {
                DisposeAll(results);
            }
        })).ToEither(e => Error.New($"Could not get data for Hyper-V network adapter '{adapterId}'.", e))
        select portName;

    private EitherAsync<Error, Unit> WaitForJob(string jobPath) =>
        TryAsync(async () =>
        {
            var stopwatch = Stopwatch.StartNew();
            var job = new ManagementObject(jobPath);
            try
            {
                job.Get();
                while (IsJobRunning((ushort)job["JobState"]) && stopwatch.Elapsed <= timeOut)
                {
                    await Task.Delay(pollingInterval)
                        .ConfigureAwait(false);
                    job.Get();
                }

                if (!IsJobCompleted((ushort)job["JobState"]))
                    throw Error.New("The job did not complete successfully within the allotted time."
                                    + $"The last reported state was {ConvertJobState((ushort)job["JobState"])}.");

                return unit;
            }
            finally
            {
                job.Dispose();
            }
        }).ToEither(e => Error.New($"Failed to wait for completion of the job '{jobPath}'.", e));

    private static EitherAsync<Error, string> ExtractAdapterId(
        string instanceId) =>
        from _1 in guard(notEmpty(instanceId),
                Error.New("The instance ID is null or empty."))
            .ToEitherAsync()
        let unescaped = instanceId.Replace(@"\\", @"\")
        let parts = unescaped.Split('\\')
        from _2 in guard(parts.Length >= 2,
            Error.New($"The instance ID '{instanceId}' does not contain a valid adapter ID."))
        let adapterId = $@"{parts[0]}\{parts[1]}"
        from _3 in guard(IsValidAdapterId(adapterId),
            Error.New($"The instance ID '{instanceId}' does not contain a valid adapter ID."))
        select adapterId;

    private static bool IsValidAdapterId(string adapterId)
    {
        if(string.IsNullOrWhiteSpace(adapterId))
            return false;

        if (!adapterId.StartsWith("Microsoft:", StringComparison.OrdinalIgnoreCase))
            return false;

        var parts = adapterId.Substring("Microsoft:".Length).Split('\\');
        return parts.Length == 2
               && Guid.TryParse(parts[0], out _)
               && Guid.TryParse(parts[1], out _);
    }

    private static bool IsValidPortName(string portName) =>
        !string.IsNullOrWhiteSpace(portName)
        && PortNameRegex().IsMatch(portName);

    /// <summary>
    /// Dispose the <paramref name="managementObjects"/>.
    /// </summary>
    /// <remarks>
    /// The <see cref="ManagementBaseObject"/>s must be explicitly disposed
    /// as there are COM objects attached to them. Furthermore,
    /// <see cref="ManagementBaseObject.Dispose"/> does only work correctly
    /// when being invoked directly. The method is defined with the
    /// <see langwork="new"/> keyword and will not be invoked via the
    /// <see cref="IDisposable"/> interface.
    /// </remarks>
    private static void DisposeAll(IList<ManagementBaseObject> managementObjects)
    {
        foreach (var managementObject in managementObjects)
        {
            managementObject.Dispose();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        
        _disposed = true;

        if (_vmms.IsValueCreated)
            _vmms.Value.Dispose();
    }

    private static string ConvertReturnValue(uint returnValue) =>
        returnValue switch
        {
            0 => "Completed",
            1 => "Not Supported",
            2 => "Failed",
            3 => "Timeout",
            4 => "Invalid Parameter",
            5 => "Invalid State",
            6 => "Incompatible Parameters",
            4096 => "Job Started",
            _ => $"Other ({returnValue})",
        };

    private static string ConvertJobState(ushort jobState) =>
        jobState switch
        {
            2 => "New",
            3 => "Starting",
            4 => "Running",
            5 => "Suspended",
            6 => "Shutting Down",
            7 => "Completed",
            8 => "Terminated",
            9 => "Killed",
            10 => "Exception",
            11 => "Service",
            _ => $"Other ({jobState})",
        };

    private static bool IsJobRunning(ushort jobState) =>
        jobState is 2 or 3 or 4 or 5 or 6;

    private static bool IsJobCompleted(ushort jobState) =>
        jobState is 7;
}
