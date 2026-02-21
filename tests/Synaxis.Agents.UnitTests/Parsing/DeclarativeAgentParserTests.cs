// <copyright file="DeclarativeAgentParserTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.UnitTests.Parsing;

using System;
using System.Collections.Generic;
using FluentAssertions;
using Synaxis.Agents.Infrastructure.Parsing;
using Synaxis.Agents.Domain.ValueObjects;
using Xunit;

[Trait("Category", "Unit")]
public class DeclarativeAgentParserTests
{
    private readonly DeclarativeAgentParser _parser = new();

    [Fact]
    public void ParseYaml_ValidYaml_ReturnsAgentSteps()
    {
        // Arrange
        var yaml = """
            name: Test Agent
            type: declarative
            steps:
              - name: Step 1
                type: input
                prompt: Enter your name
              - name: Step 2
                type: function
                function: process_data
                arguments:
                  param1: value1
                  param2: value2
              - name: Step 3
                type: output
                message: Processing complete
            """;

        // Act
        var steps = _parser.Parse(yaml);

        // Assert
        steps.Should().HaveCount(3);
        steps[0].Name.Should().Be("Step 1");
        steps[0].Type.Should().Be(AgentStepType.Input);
        steps[0].Prompt.Should().Be("Enter your name");

        steps[1].Name.Should().Be("Step 2");
        steps[1].Type.Should().Be(AgentStepType.Function);
        steps[1].Function.Should().Be("process_data");
        steps[1].Arguments.Should().HaveCount(2);
        steps[1].Arguments!["param1"].Should().Be("value1");
        steps[1].Arguments!["param2"].Should().Be("value2");

        steps[2].Name.Should().Be("Step 3");
        steps[2].Type.Should().Be(AgentStepType.Output);
        steps[2].Message.Should().Be("Processing complete");
    }

    [Fact]
    public void ParseYaml_InvalidYaml_ThrowsException()
    {
        // Arrange
        var yaml = """
            name: Test Agent
            type: invalid_type
            steps:
              - name: Step 1
                type: input
            """;

        // Act
        Action act = () => _parser.Parse(yaml);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Unsupported agent type: invalid_type. Only 'declarative' is supported.");
    }

    [Fact]
    public void ParseYaml_EmptyYaml_ThrowsArgumentNullException()
    {
        // Arrange
        var yaml = "";

        // Act
        Action act = () => _parser.Parse(yaml);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ParseYaml_NullYaml_ThrowsArgumentNullException()
    {
        // Arrange
        string? yaml = null;

        // Act
        Action act = () => _parser.Parse(yaml!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ParseYaml_WhitespaceOnlyYaml_ThrowsArgumentNullException()
    {
        // Arrange
        var yaml = "   \n\t\n   ";

        // Act
        Action act = () => _parser.Parse(yaml);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ParseYaml_MissingName_ThrowsInvalidOperationException()
    {
        // Arrange
        var yaml = """
            type: declarative
            steps:
              - name: Step 1
                type: input
            """;

        // Act
        Action act = () => _parser.Parse(yaml);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Agent configuration must have a name");
    }

    [Fact]
    public void ParseYaml_MissingSteps_ThrowsInvalidOperationException()
    {
        // Arrange
        var yaml = """
            name: Test Agent
            type: declarative
            """;

        // Act
        Action act = () => _parser.Parse(yaml);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Agent configuration must have at least one step");
    }

    [Fact]
    public void ParseYaml_EmptySteps_ThrowsInvalidOperationException()
    {
        // Arrange
        var yaml = """
            name: Test Agent
            type: declarative
            steps: []
            """;

        // Act
        Action act = () => _parser.Parse(yaml);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Agent configuration must have at least one step");
    }

    [Fact]
    public void ParseYaml_StepWithoutName_ThrowsInvalidOperationException()
    {
        // Arrange
        var yaml = """
            name: Test Agent
            type: declarative
            steps:
              - type: input
                prompt: Enter name
            """;

        // Act
        Action act = () => _parser.Parse(yaml);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("All steps must have a name");
    }

    [Fact]
    public void ParseYaml_InvalidStepType_ThrowsInvalidOperationException()
    {
        // Arrange
        var yaml = """
            name: Test Agent
            type: declarative
            steps:
              - name: Step 1
                type: invalid_type
            """;

        // Act
        Action act = () => _parser.Parse(yaml);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Invalid step type: invalid_type");
    }

    [Fact]
    public void ParseYaml_AllStepTypes_ParsesCorrectly()
    {
        // Arrange
        var yaml = """
            name: Test Agent
            type: declarative
            steps:
              - name: Input Step
                type: input
                prompt: Enter value
                output_variable: user_input
              - name: Function Step
                type: function
                function: my_function
                arguments:
                  arg1: value1
              - name: Output Step
                type: output
                message: "Result: user_input"
              - name: Condition Step
                type: condition
                condition: "user_input == yes"
              - name: Loop Step
                type: loop
                collection: items
                output_variable: item
            """;

        // Act
        var steps = _parser.Parse(yaml);

        // Assert
        steps.Should().HaveCount(5);

        steps[0].Type.Should().Be(AgentStepType.Input);
        steps[0].Prompt.Should().Be("Enter value");
        steps[0].OutputVariable.Should().Be("user_input");

        steps[1].Type.Should().Be(AgentStepType.Function);
        steps[1].Function.Should().Be("my_function");

        steps[2].Type.Should().Be(AgentStepType.Output);
        steps[2].Message.Should().Be("Result: user_input");

        steps[3].Type.Should().Be(AgentStepType.Condition);
        steps[3].Condition.Should().Be("user_input == yes");

        steps[4].Type.Should().Be(AgentStepType.Loop);
        steps[4].Collection.Should().Be("items");
        steps[4].OutputVariable.Should().Be("item");
    }

    [Fact]
    public void ParseYaml_CaseInsensitiveStepType_ParsesCorrectly()
    {
        // Arrange
        var yaml = """
            name: Test Agent
            type: declarative
            steps:
              - name: Step 1
                type: INPUT
              - name: Step 2
                type: Function
              - name: Step 3
                type: OUTPUT
            """;

        // Act
        var steps = _parser.Parse(yaml);

        // Assert
        steps.Should().HaveCount(3);
        steps[0].Type.Should().Be(AgentStepType.Input);
        steps[1].Type.Should().Be(AgentStepType.Function);
        steps[2].Type.Should().Be(AgentStepType.Output);
    }

    [Fact]
    public void ParseYaml_WithDescription_ParsesCorrectly()
    {
        // Arrange
        var yaml = """
            name: Test Agent
            description: A test agent for unit testing
            type: declarative
            steps:
              - name: Step 1
                type: input
            """;

        // Act
        var steps = _parser.Parse(yaml);

        // Assert
        steps.Should().HaveCount(1);
        steps[0].Name.Should().Be("Step 1");
    }
}
