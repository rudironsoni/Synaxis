// <copyright file="DeclarativeAgentParser.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Infrastructure.Parsing;

using System.Collections.Generic;
using Synaxis.Agents.Domain.ValueObjects;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

/// <summary>
/// Parses YAML agent definitions into AgentStep collections.
/// </summary>
public sealed class DeclarativeAgentParser
{
    private readonly IDeserializer _deserializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeclarativeAgentParser"/> class.
    /// </summary>
    public DeclarativeAgentParser()
    {
        this._deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <summary>
    /// Parses a YAML string into a collection of AgentStep objects.
    /// </summary>
    /// <param name="yaml">The YAML string to parse.</param>
    /// <returns>The parsed collection of AgentStep objects.</returns>
    /// <exception cref="ArgumentNullException">Thrown when yaml is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the YAML is invalid.</exception>
    public IReadOnlyList<AgentStep> Parse(string yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
        {
            throw new ArgumentNullException(nameof(yaml));
        }

        var yamlConfig = this._deserializer.Deserialize<YamlAgentConfiguration>(yaml);
        ValidateConfiguration(yamlConfig);

        return ConvertSteps(yamlConfig.Steps ?? []);
    }

    /// <summary>
    /// Parses a YAML file into a collection of AgentStep objects.
    /// </summary>
    /// <param name="filePath">The path to the YAML file.</param>
    /// <returns>The parsed collection of AgentStep objects.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file is not found.</exception>
    public async Task<IReadOnlyList<AgentStep>> ParseFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Agent configuration file not found: {filePath}");
        }

        var yaml = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
        return this.Parse(yaml);
    }

    private static void ValidateConfiguration(YamlAgentConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(config.Name))
        {
            throw new InvalidOperationException("Agent configuration must have a name");
        }

        if (!string.Equals(config.Type, "declarative", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Unsupported agent type: {config.Type}. Only 'declarative' is supported.");
        }

        if (config.Steps == null || config.Steps.Count == 0)
        {
            throw new InvalidOperationException("Agent configuration must have at least one step");
        }

        foreach (var step in config.Steps)
        {
            if (string.IsNullOrWhiteSpace(step.Name))
            {
                throw new InvalidOperationException("All steps must have a name");
            }

            if (!Enum.TryParse<AgentStepType>(step.Type, true, out _))
            {
                throw new InvalidOperationException($"Invalid step type: {step.Type}");
            }
        }
    }

    private static IReadOnlyList<AgentStep> ConvertSteps(List<YamlAgentStep> yamlSteps)
    {
        var steps = new List<AgentStep>(yamlSteps.Count);

        foreach (var yamlStep in yamlSteps)
        {
            var stepType = Enum.Parse<AgentStepType>(yamlStep.Type, true);

            var step = new AgentStep
            {
                Name = yamlStep.Name ?? string.Empty,
                Type = stepType,
                Prompt = yamlStep.Prompt,
                Function = yamlStep.Function,
                Arguments = yamlStep.Arguments,
                Message = yamlStep.Message,
                Condition = yamlStep.Condition,
                Collection = yamlStep.Collection,
                OutputVariable = yamlStep.OutputVariable,
            };

            steps.Add(step);
        }

        return steps.AsReadOnly();
    }

    private sealed class YamlAgentConfiguration
    {
        public string? Name { get; set; }

        public string? Description { get; set; }

        public string Type { get; set; } = "declarative";

        public List<YamlAgentStep>? Steps { get; set; }
    }

    private sealed class YamlAgentStep
    {
        public string? Name { get; set; }

        public string Type { get; set; } = string.Empty;

        public string? Prompt { get; set; }

        public string? Function { get; set; }

        public Dictionary<string, string>? Arguments { get; set; }

        public string? Message { get; set; }

        public string? Condition { get; set; }

        public string? Collection { get; set; }

        public string? OutputVariable { get; set; }
    }
}
