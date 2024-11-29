﻿using LanguageExt;
using LanguageExt.Common;

namespace Dbosoft.OVN;

/// <summary>
/// This manager assists with managing OVS port on Hyper-V.
/// </summary>
public interface IHyperVOvsPortManager : IDisposable
{
    /// <summary>
    /// Returns the OVS port name which is assigned to the Hyper-V
    /// network adapter with the given <paramref name="adapterId"/>
    /// </summary>
    EitherAsync<Error, Option<string>> GetPortName(
        string adapterId);

    /// <summary>
    /// Returns IDs of the Hyper-V network adapters which are
    /// assigned the given <paramref name="portName"/> in OVS.
    /// </summary>
    /// <remarks>
    /// An OVS port name should be assigned to at most one Hyper-V
    /// network adapter. When this method returns multiple result,
    /// the OVS configuration is invalid.
    /// </remarks>
    EitherAsync<Error, Seq<string>> GetAdapterIds(
        string portName);

    /// <summary>
    /// Sets the OVS port name of the Hyper-V network adapter with
    /// the given <paramref name="adapterId"/> to the given
    /// <paramref name="portName"/>.
    /// </summary>
    /// <remarks>
    /// It is possible that Hyper-V starts a job to apply the port name.
    /// In that case, this operation might take a while or even time out.
    /// </remarks>
    EitherAsync<Error, Unit> SetPortName(
        string adapterId,
        string portName);
}