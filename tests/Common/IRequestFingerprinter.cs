// <copyright file="IRequestFingerprinter.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Common.Tests;

using Microsoft.Extensions.AI;

/// <summary>
/// Interface for request fingerprinting.
/// This is a test stub - replace with actual implementation reference when available.
/// </summary>
public interface IRequestFingerprinter
{
    string GenerateFingerprint(IEnumerable<ChatMessage> messages, ChatOptions? options);
}
