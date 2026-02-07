// <copyright file="GlobalSuppressions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "StyleCop.CSharp.DocumentationRules",
    "SA1649:File name should match first type name",
    Justification = "StyleCop crashes on record declarations (AD0001 bug)",
    Scope = "namespaceanddescendants",
    Target = "~N:Synaxis.InferenceGateway")]
