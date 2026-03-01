// <copyright file="ArchivalResult.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services
{
    /// <summary>
    /// Represents the result of an archival operation.
    /// </summary>
    /// <param name="ArchivedCount">The number of logs archived.</param>
    /// <param name="ArchivePath">The path to the archive file, or null if no logs were archived.</param>
    public record ArchivalResult(int ArchivedCount, string? ArchivePath);
}
