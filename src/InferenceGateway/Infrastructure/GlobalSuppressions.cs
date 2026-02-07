// <copyright file="GlobalSuppressions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using System.Diagnostics.CodeAnalysis;

// SA1011 conflicts with SA1018 for nullable array syntax (string[]?)
// Modern C# convention is string[]? without space, which violates SA1011
[assembly: SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square bracket should be followed by a space", Justification = "Conflicts with SA1018 for nullable arrays; string[]? is correct modern syntax")]
