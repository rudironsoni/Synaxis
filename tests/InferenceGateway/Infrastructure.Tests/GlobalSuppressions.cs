// <copyright file="GlobalSuppressions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using System.Diagnostics.CodeAnalysis;

// Suppress StyleCop rules that are less critical for test projects
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA0001:XML comment analysis is disabled", Justification = "XML documentation not required for tests")]
[assembly: SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1200:Using directives should be placed correctly", Justification = "Using directives at file level for test clarity")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1633:File should have header", Justification = "File headers not required for test files")]
[assembly: SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1210:Using directives should be ordered alphabetically", Justification = "Using order not critical for tests")]
[assembly: SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1124:Do not use regions", Justification = "Regions allowed for test organization")]
[assembly: SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1515:Single-line comment should be preceded by blank line", Justification = "Comment layout flexibility for tests")]
[assembly: SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1122:Use string.Empty for empty strings", Justification = "\"\" is clear and concise in tests")]
[assembly: SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1513:Closing brace should be followed by blank line", Justification = "Layout flexibility for tests")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Test helpers and DTOs can be in same file")]
[assembly: SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1501:Statement should not be on a single line", Justification = "Compact test statements allowed")]
[assembly: SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1009:Closing parenthesis should be spaced correctly", Justification = "Spacing flexibility for tests")]
[assembly: SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1116:Split parameters should start on line after declaration", Justification = "Parameter layout flexibility for tests")]
[assembly: SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1117:Parameters should be on same line or separate lines", Justification = "Parameter layout flexibility for tests")]
[assembly: SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:Parameter should not span multiple lines", Justification = "Parameter layout flexibility for tests")]
[assembly: SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1512:Single-line comments should not be followed by blank line", Justification = "Comment layout flexibility for tests")]
[assembly: SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1518:Use line endings correctly at end of file", Justification = "End of file formatting not critical")]
[assembly: SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1507:Code should not contain multiple blank lines in a row", Justification = "Blank line flexibility for test organization")]
[assembly: SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1111:Closing parenthesis should be on line of last parameter", Justification = "Closing paren layout flexibility")]
[assembly: SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "Spacing flexibility for tests")]
[assembly: SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1028:Code should not contain trailing whitespace", Justification = "Trailing whitespace not critical in tests")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1413:Use trailing comma in multi-line initializers", Justification = "Trailing commas optional in tests")]
[assembly: SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1502:Element should not be on a single line", Justification = "Compact elements allowed in tests")]
[assembly: SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1208:System using directives should be placed before other using directives", Justification = "Using order not critical")]
[assembly: SuppressMessage("StyleCop.CSharp.NamingRules", "SA1306:Field names should begin with lower-case letter", Justification = "Field naming flexibility for tests")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "Test helper files may contain multiple types")]
[assembly: SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1107:Code should not contain multiple statements on one line", Justification = "Multiple statements allowed for concise tests")]

// Suppress Meziantou analyzer rules that are less critical for test projects
[assembly: SuppressMessage("Style", "MA0004:Use ConfigureAwait(false)", Justification = "ConfigureAwait not critical in test code")]
[assembly: SuppressMessage("Style", "MA0006:Use String.Equals", Justification = "== operator is clear in tests")]
[assembly: SuppressMessage("Design", "MA0011:IFormatProvider is missing", Justification = "Culture not critical in tests")]
[assembly: SuppressMessage("Style", "MA0015:Specify the parameter name in ArgumentException", Justification = "Parameter names optional in tests")]
[assembly: SuppressMessage("Style", "MA0016:Prefer return collection abstraction instead of implementation", Justification = "Concrete types acceptable in tests")]
[assembly: SuppressMessage("Performance", "MA0026:Fix TODO comment", Justification = "TODO comments allowed in tests")]
[assembly: SuppressMessage("Design", "MA0048:File name must match type name", Justification = "Test helper files may have different names")]
[assembly: SuppressMessage("Design", "MA0051:Method is too long", Justification = "Long test methods acceptable for comprehensive testing")]
[assembly: SuppressMessage("Usage", "MA0074:Avoid implicit culture-sensitive methods", Justification = "String operations with default culture acceptable in tests")]
[assembly: SuppressMessage("Usage", "MA0132:Do not convert implicitly to DateTimeOffset", Justification = "Implicit conversions acceptable in tests")]
[assembly: SuppressMessage("Usage", "MA0002:IEqualityComparer<string> or IComparer<string> is missing", Justification = "Default comparers acceptable in tests")]

// Suppress IDisposableAnalyzer rules for test projects
[assembly: SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP001:Dispose created", Justification = "Test disposables managed by test framework or not critical")]
[assembly: SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP003:Dispose previous before re-assigning", Justification = "Test cleanup patterns acceptable")]
[assembly: SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP004:Don't ignore created IDisposable", Justification = "Test objects disposal not critical")]
[assembly: SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP005:Return type should indicate that the value should be disposed", Justification = "Test helper return types flexibility")]
[assembly: SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP006:Implement IDisposable", Justification = "Test classes disposal patterns acceptable")]
[assembly: SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP017:Prefer using", Justification = "Test disposal patterns can use try-finally")]
[assembly: SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP025:Class with no virtual dispose method should be sealed", Justification = "Test classes sealing not required")]

// Suppress AsyncFixer rules for test projects
[assembly: SuppressMessage("AsyncUsage", "AsyncFixer01:Unnecessary async/await usage", Justification = "Async/await clarity in tests preferred over micro-optimization")]

// Suppress SonarAnalyzer rules for test projects
[assembly: SuppressMessage("Minor Code Smell", "S108:Nested blocks of code should not be left empty", Justification = "Empty blocks acceptable in test setup")]
[assembly: SuppressMessage("Minor Code Smell", "S125:Sections of code should not be commented out", Justification = "Commented code acceptable for test examples")]
[assembly: SuppressMessage("Minor Code Smell", "S1135:Track uses of 'TODO' tags", Justification = "TODO tags acceptable in tests")]
[assembly: SuppressMessage("Minor Code Smell", "S1481:Unused local variables should be removed", Justification = "Variables for test clarity may appear unused")]
[assembly: SuppressMessage("Major Code Smell", "S2699:Tests should include assertions", Justification = "Some tests verify no exceptions thrown")]
[assembly: SuppressMessage("Critical Code Smell", "S927:Parameter names should match base declaration and other partial definitions", Justification = "Parameter names flexibility in tests")]
[assembly: SuppressMessage("Minor Code Smell", "S3878:Arrays should not be created for params parameters", Justification = "Explicit arrays acceptable for clarity")]
[assembly: SuppressMessage("Major Code Smell", "S3881:IDisposable should be implemented correctly", Justification = "Test disposable patterns may be simplified")]
[assembly: SuppressMessage("Minor Code Smell", "S3928:Parameter names used into ArgumentException constructors should match an existing one", Justification = "Parameter names optional in test exceptions")]
[assembly: SuppressMessage("Major Code Smell", "S4144:Methods should not have identical implementations", Justification = "Test methods may have similar structure")]
[assembly: SuppressMessage("Info Code Smell", "S6608:Prefer indexing instead of Enumerable methods on IndexableCollections", Justification = "LINQ methods acceptable for test readability")]
