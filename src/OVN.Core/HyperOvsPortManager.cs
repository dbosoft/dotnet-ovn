using System;
using System.Collections.Generic;
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

[SupportedOSPlatform("windows")]
public sealed partial class HyperOvsPortManager : IDisposable
{
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

    public EitherAsync<Error, Option<string>> GetOvsPortName(string adapterId) =>
        from _ in guard(IsValidAdapterId(adapterId),
                Error.New($"The Hyper-V network adapter ID '{adapterId}' is invalid."))
            .ToEitherAsync()
        from adapterInfo in GetAdapterInfo(adapterId)
        let portName = adapterInfo.Map(i => i.ElementName)
        select portName;

    public EitherAsync<Error, Option<string>> GetAdapterId(string portName) =>
        from _ in guard(IsValidPortName(portName),
                Error.New($"The OVS port name '{portName}' is invalid."))
            .ToEitherAsync()
        from instanceId in TryAsync(Task.Factory.StartNew(() =>
            {
                using var searcher = new ManagementObjectSearcher(
                    new ManagementScope(Scope),
                    new ObjectQuery("SELECT ElementName "
                                    + "FROM Msvm_EthernetPortAllocationSettingData "
                                    + $"WHERE ElementName = '{portName}'"));

                using var collection = searcher.Get();
                var results = collection.Cast<ManagementBaseObject>().ToList();
                try
                {
                    return results.Count == 0
                        ? Option<string>.None
                        : Optional((string)results.Single()["InstanceID"]);
                }
                finally
                {
                    DisposeAll(results);
                }
            }, TaskCreationOptions.LongRunning))
            .ToEither(e => Error.New($"Could not get adapter ID for OVS port name '{portName}'.", e))
        select instanceId;

    public EitherAsync<Error, Unit> SetOvsPortName(string adapterId, string portName) =>
        from adapterInfo in GetAdapterInfo(adapterId)
        from validAdapterInfo in adapterInfo.ToEitherAsync(
            Error.New($"The Hyper-V network adapter '{adapterId}' does not exist."))
        from _2 in guard(IsValidPortName(portName),
            Error.New($"The OVS port name '{portName}' is invalid."))
        from __3 in TryAsync(Task.Factory.StartNew(() =>
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

                throw Error.New("Invalid response");
                // TODO Check result and job status
                return unit;
            }
            finally
            {
                adapterData?.Dispose();
                parameters?.Dispose();
                result?.Dispose();
            }
        })).ToEither(e => Error.New($"Could not set OVS port name for adapter '{adapterId}'.", e))
        // TODO Query job progress
        select unit;

    private EitherAsync<Error, Option<(string Path, string ElementName)>> GetAdapterInfo(string adapterId) =>
        from _ in guard(IsValidAdapterId(adapterId),
                Error.New($"The Hyper-V network adapter ID '{adapterId}' is invalid."))
            .ToEitherAsync()
        from portName in TryAsync(Task.Factory.StartNew(() =>
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
            }, TaskCreationOptions.LongRunning))
            .ToEither(ex => Error.New($"Could not get data for Hyper-V network adapter '{adapterId}'.", Error.New(ex)))
        select portName;

    private bool IsValidAdapterId(string adapterId)
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

    private bool IsValidPortName(string portName) =>
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
    private void DisposeAll(IList<ManagementBaseObject> managementObjects)
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

    private enum ModifyResourceSettingsResult
    {

    }
}
