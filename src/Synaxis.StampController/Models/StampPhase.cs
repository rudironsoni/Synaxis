// <copyright file="StampPhase.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.StampController.Models;

/// <summary>
/// Defines the phases of a stamp lifecycle.
/// </summary>
public enum StampPhase
{
    /// <summary>
    /// Stamp is being provisioned.
    /// </summary>
    Provision,

    /// <summary>
    /// Stamp is being registered in the system.
    /// </summary>
    Register,

    /// <summary>
    /// Stamp is active and serving traffic.
    /// </summary>
    Active,

    /// <summary>
    /// Stamp is being drained of existing connections.
    /// </summary>
    Drain,

    /// <summary>
    /// Stamp is quarantined due to issues.
    /// </summary>
    Quarantine,

    /// <summary>
    /// Stamp is being decommissioned.
    /// </summary>
    Decommission,

    /// <summary>
    /// Stamp is archived for potential restoration.
    /// </summary>
    Archive,

    /// <summary>
    /// Stamp is being permanently removed.
    /// </summary>
    Purge,
}
