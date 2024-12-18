﻿using System.Diagnostics;
using System.Management;
using System.Text.RegularExpressions;
using LanguageExt;
using LanguageExt.Common;

using static LanguageExt.Prelude;

namespace Dbosoft.OVN.Windows;

/// <inheritdoc cref="IHyperVOvsPortManager"/>
public sealed partial class HyperVOvsPortManager(
    TimeSpan timeOut,
    TimeSpan pollingInterval)
    : IHyperVOvsPortManager
{
    /// <inheritdoc cref="IHyperVOvsPortManager"/>
    public HyperVOvsPortManager() : this(TimeSpan.FromMinutes(1), TimeSpan.FromMilliseconds(100))
    {
    }

    [GeneratedRegex("^[a-zA-Z0-9-_]+$")]
    private static partial Regex PortNameRegex();
    private const string Scope = @"root\virtualization\v2";
    private const string PortNamePrefix = "ovs_";

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

    // The ManagementBaseObjects must be explicitly disposed as they
    // hold COM objects. Furthermore, ManagementBaseObject.Dispose()
    // does only work correctly when being invoked directly.
    // The method is defined with the new keyword and will not be invoked
    // via the IDisposable interface (e.g. with a using statement).

    /// <inheritdoc/>
    public EitherAsync<Error, string> GetPortName(string adapterId) =>
        from _ in guard(IsValidAdapterId(adapterId),
                Error.New($"The Hyper-V network adapter ID '{adapterId}' is invalid."))
            .ToEitherAsync()
        from adapterInfo in GetAdapterInfo(adapterId)
        from validAdapterInfo in adapterInfo.ToEitherAsync(
            Error.New($"The Hyper-V network adapter '{adapterId}' does not exist."))
        let portName = validAdapterInfo.ElementName ?? ""
        select portName;

    /// <inheritdoc/>
    public EitherAsync<Error, Option<string>> GetConfiguredPortName(string adapterId) =>
        from portName in GetPortName(adapterId)
        select Optional(portName).Filter(IsValidPortName).Filter(p => p.StartsWith(PortNamePrefix));

    /// <inheritdoc/>
    public EitherAsync<Error, Seq<(string AdapterId, string PortName)>> GetPortNames() =>
        from data in TryAsync(Task.Run(() =>
        {
            using var searcher = new ManagementObjectSearcher(
                new ManagementScope(Scope),
                new ObjectQuery("SELECT InstanceID, ElementName "
                                + "FROM Msvm_EthernetPortAllocationSettingData"));

            using var collection = searcher.Get();
            var results = collection.Cast<ManagementBaseObject>().ToList();
            try
            {
                return results
                    .Map(r => (InstanceId: (string)r["InstanceID"], ElementName: (string)r["ElementName"] ?? ""))
                    // Invoke ToList() to force eager evaluation of the LINQ query
                    .ToList().ToSeq();
            }
            finally
            {
                DisposeAll(results);
            }
        })).ToEither(e => Error.New("Could not get data for the Hyper-V network adapters.", e))
        from portNames in data
            // Filter out default settings which do not represent actual adapters
            .Filter(t => !t.InstanceId.StartsWith("Microsoft:Definition"))
            .Map(t => from adapterId in ExtractAdapterId(t.InstanceId)
                      select (AdapterId: adapterId, PortName: t.ElementName))
            .SequenceSerial()
        select portNames;

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
            .ToEither(e => Error.New($"Could not get the adapter IDs for OVS port name '{portName}'.",e))
        from adapterIds in instanceIds.Map(ExtractAdapterId).SequenceSerial()
        select adapterIds;

    /// <inheritdoc/>
    public EitherAsync<Error, Unit> SetPortName(
        string adapterId,
        string portName) =>
        from adapterInfo in GetAdapterInfo(adapterId)
        from validAdapterInfo in adapterInfo.ToEitherAsync(
            Error.New($"The Hyper-V network adapter '{adapterId}' does not exist."))
        from _1 in guard(IsValidPortName(portName),
            Error.New($"The OVS port name '{portName}' is invalid."))
        from _2 in guard(portName.StartsWith(PortNamePrefix),
            Error.New($"The OVS port name must start with '{PortNamePrefix}'."))
        from jobPath in AffMaybe(async () => await Task.Run(() =>
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
                    0 => FinSucc(Option<string>.None),
                    4096 => Some((string)result["Job"]),
                    _ => Error.New($"ModifyResourceSettings failed with result '{ConvertReturnValue(returnValue)}'"),
                };
            }
            finally
            {
                adapterData?.Dispose();
                parameters?.Dispose();
                result?.Dispose();
            }
        })).MapFail(e => Error.New($"Could not set the OVS port name of the adapter '{adapterId}'.", e))
            .Run().AsTask().Map(r => r.ToEither()).ToAsync()
        from _4 in jobPath.Map(WaitForJob).SequenceSerial()
        from reportedPortName in GetPortName(adapterId)
        from _5 in guard(reportedPortName == portName,
            Error.New($"The OVS port name has not been properly set for the adapter '{adapterId}'"))
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
        })).ToEither(e => Error.New($"Could not get data for the Hyper-V network adapter '{adapterId}'.", e))
        select portName;

    private EitherAsync<Error, Unit> WaitForJob(string jobPath) =>
        AffMaybe<Unit>(async () =>
        {
            var stopwatch = Stopwatch.StartNew();
            var job = new ManagementObject(jobPath);
            try
            {
                job.Get();
                while (IsJobRunning((ushort)job["JobState"]) && stopwatch.Elapsed <= timeOut)
                {
                    await Task.Delay(pollingInterval).ConfigureAwait(false);
                    job.Get();
                }

                if (!IsJobCompleted((ushort)job["JobState"]))
                    return Error.New("The job did not complete successfully within the allotted time."
                                     + $"The last reported state was {ConvertJobState((ushort)job["JobState"])}.");

                return unit;
            }
            finally
            {
                job.Dispose();
            }
        }).MapFail(e => Error.New($"Failed to wait for the completion of the job '{jobPath}'.", e))
            .Run().AsTask().Map(r => r.ToEither()).ToAsync();

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
        if (string.IsNullOrWhiteSpace(adapterId))
            return false;

        if (!adapterId.StartsWith("Microsoft:", StringComparison.OrdinalIgnoreCase))
            return false;

        var parts = adapterId["Microsoft:".Length..].Split('\\');
        return parts.Length == 2
               && Guid.TryParse(parts[0], out _)
               && Guid.TryParse(parts[1], out _);
    }

    private static bool IsValidPortName(string portName) =>
        notEmpty(portName) && PortNameRegex().IsMatch(portName);

    private static void DisposeAll(IList<ManagementBaseObject> managementObjects)
    {
        foreach (var managementObject in managementObjects)
        {
            managementObject.Dispose();
        }
    }

    /// <inheritdoc/>
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
