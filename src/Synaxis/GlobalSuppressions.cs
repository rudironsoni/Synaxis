// <copyright file="GlobalSuppressions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using System.Diagnostics.CodeAnalysis;

// Temporary suppressions for incomplete implementation
[assembly: SuppressMessage("Performance", "MA0026:Fix TODO comment", Justification = "Framework implementation is intentionally incomplete pending provider infrastructure")]
[assembly: SuppressMessage("Minor Code Smell", "S1135:Track uses of 'TODO' tags", Justification = "Framework implementation is intentionally incomplete pending provider infrastructure")]
[assembly: SuppressMessage("Critical Code Smell", "S2139:Exceptions should be either logged or rethrown but not both", Justification = "Logging before rethrow provides context for pipeline failures")]
[assembly: SuppressMessage("Major Code Smell", "S4456:Parameter validation should not be in iterator", Justification = "Mediator framework handles validation appropriately")]
[assembly: SuppressMessage("AsyncUsage", "AsyncFixer01:Unnecessary async/await", Justification = "Async/await maintained for consistent API pattern")]
[assembly: SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1009:Closing parenthesis should not be followed by a space", Justification = "Record syntax with multiple interfaces requires space")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Related types grouped for cohesion")]
[assembly: SuppressMessage("Design", "MA0048:File name must match type name", Justification = "Related types grouped for cohesion")]
[assembly: SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1512:Single-line comments should not be followed by blank line", Justification = "Comment formatting preference")]
