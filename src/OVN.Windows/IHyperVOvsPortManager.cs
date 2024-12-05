using LanguageExt;
using LanguageExt.Common;

namespace Dbosoft.OVN.Windows;

/// <summary>
/// This manager assists with managing OVS ports in Hyper-V.
/// </summary>
public interface IHyperVOvsPortManager : IDisposable
{
    /// <summary>
    /// Returns the OVS port name which is assigned to the Hyper-V
    /// network adapter with the given <paramref name="adapterId"/>.
    /// </summary>
    /// <remarks>
    /// Consider using <see cref="GetConfiguredPortName"/> if you want
    /// to check that a port name has been explicitly configured with
    /// <see cref="SetPortName"/>.
    /// </remarks>
    EitherAsync<Error, string> GetPortName(
        string adapterId);

    /// <summary>
    /// Returns the OVS port name which is assigned to the Hyper-V network
    /// adapter with the given <paramref name="adapterId"/>. This method
    /// returns <see cref="OptionNone"/> if the port name has not been
    /// explicitly configured.
    /// </summary>
    /// <remarks>
    /// Hyper-V populates the corresponding field with a default value.
    /// This method detects if the value was not set explicitly (e.g.
    /// it does not start with <c>ovs_</c>) and then considers the port
    /// name as not configured and returns <see cref="OptionNone"/>.
    /// </remarks>
    EitherAsync<Error, Option<string>> GetConfiguredPortName(
        string adapterId);

    /// <summary>
    /// Returns a list of all Hyper-V network adapters and their
    /// OVS port names.
    /// </summary>
    EitherAsync<Error, Seq<(string AdapterId, string PortName)>> GetPortNames();

    /// <summary>
    /// Returns the IDs of the Hyper-V network adapters which are
    /// assigned the given <paramref name="portName"/> in OVS.
    /// </summary>
    /// <remarks>
    /// An OVS port name should be assigned to at most one Hyper-V
    /// network adapter. When this method returns multiple results,
    /// the OVS configuration is invalid.
    /// </remarks>
    EitherAsync<Error, Seq<string>> GetAdapterIds(
        string portName);

    /// <summary>
    /// Sets the OVS port name of the Hyper-V network adapter with
    /// the given <paramref name="adapterId"/> to the given
    /// <paramref name="portName"/>. The <paramref name="portName"/>
    /// must start with <c>ovs_</c>.
    /// </summary>
    /// <remarks>
    /// It is possible that Hyper-V starts a job to apply the port name.
    /// In that case, this operation might take a while or even time out.
    /// </remarks>
    EitherAsync<Error, Unit> SetPortName(
        string adapterId,
        string portName);
}
