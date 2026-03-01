// <copyright file="ExportResult.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Application.Services.Audit;

/// <summary>
/// Result of an export operation.
/// </summary>
/// <param name="FilePath">The path to the exported file.</param>
/// <param name="RecordCount">The number of records exported.</param>
/// <param name="FileSizeBytes">The size of the exported file in bytes.</param>
public record ExportResult(
    string FilePath,
    int RecordCount,
    long FileSizeBytes);
