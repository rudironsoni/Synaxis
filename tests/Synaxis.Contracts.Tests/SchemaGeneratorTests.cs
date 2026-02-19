using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;

namespace Synaxis.Contracts.Tests;

public class SchemaGeneratorTests
{
    private readonly SchemaGenerator _generator = new();

    [Fact]
    public void GenerateSchema_ForUserDto_ShouldReturnValidSchema()
    {
        var schema = _generator.GenerateSchema<global::Synaxis.Contracts.V1.DTOs.UserDto>();

        schema.Should().NotBeNullOrEmpty();

        var jsonNode = JsonNode.Parse(schema);
        jsonNode.Should().NotBeNull();
        // Schema should contain type information
        jsonNode.ToJsonString().Should().Contain("\"type\"");
    }

    [Fact]
    public void GenerateSchema_ForCreateUserCommand_ShouldReturnValidSchema()
    {
        var schema = _generator.GenerateSchema<global::Synaxis.Contracts.V1.Commands.CreateUserCommand>();

        schema.Should().NotBeNullOrEmpty();

        var jsonNode = JsonNode.Parse(schema);
        jsonNode.Should().NotBeNull();
    }

    [Fact]
    public void GenerateSchema_ForUserCreatedEvent_ShouldReturnValidSchema()
    {
        var schema = _generator.GenerateSchema<global::Synaxis.Contracts.V1.DomainEvents.UserCreated>();

        schema.Should().NotBeNullOrEmpty();

        var jsonNode = JsonNode.Parse(schema);
        jsonNode.Should().NotBeNull();
    }

    [Fact(Skip = "Polymorphic generic types are not supported by JSON Schema exporter")]
    public void GenerateSchema_ForPaginatedResult_ShouldReturnValidSchema()
    {
        var schema = _generator.GenerateSchema(typeof(global::Synaxis.Contracts.V1.DTOs.PaginatedResult<global::Synaxis.Contracts.V1.DTOs.UserDto>));

        schema.Should().NotBeNullOrEmpty();

        var jsonNode = JsonNode.Parse(schema);
        jsonNode.Should().NotBeNull();
    }

    [Fact]
    public void GenerateSchema_ForV2UserDto_ShouldReturnValidSchema()
    {
        var schema = _generator.GenerateSchema<global::Synaxis.Contracts.V2.DTOs.UserDto>();

        schema.Should().NotBeNullOrEmpty();

        var jsonNode = JsonNode.Parse(schema);
        jsonNode.Should().NotBeNull();
    }

    [Fact]
    public void GenerateV1Schemas_ShouldCreateSchemaFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            var schemas = _generator.GenerateV1Schemas(tempDir);

            schemas.Should().NotBeEmpty();
            Directory.Exists(tempDir).Should().BeTrue();

            foreach (var filePath in schemas.Values)
            {
                File.Exists(filePath).Should().BeTrue();
                var content = File.ReadAllText(filePath);
                content.Should().NotBeNullOrEmpty();

                // Verify it's valid JSON
                var jsonNode = JsonNode.Parse(content);
                jsonNode.Should().NotBeNull();
            }
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public void GenerateV2Schemas_ShouldCreateSchemaFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            var schemas = _generator.GenerateV2Schemas(tempDir);

            schemas.Should().NotBeEmpty();
            Directory.Exists(tempDir).Should().BeTrue();

            foreach (var filePath in schemas.Values)
            {
                File.Exists(filePath).Should().BeTrue();
                var content = File.ReadAllText(filePath);
                content.Should().NotBeNullOrEmpty();

                // Verify it's valid JSON
                var jsonNode = JsonNode.Parse(content);
                jsonNode.Should().NotBeNull();
            }
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public void GenerateSchema_ForEnum_ShouldReturnValidSchema()
    {
        var schema = _generator.GenerateSchema<global::Synaxis.Contracts.V1.Common.UserStatus>();

        schema.Should().NotBeNullOrEmpty();

        var jsonNode = JsonNode.Parse(schema);
        jsonNode.Should().NotBeNull();
    }

    [Fact]
    public void GenerateSchema_WithNullType_ShouldThrowArgumentNullException()
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        Action act = () => _generator.GenerateSchema(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GenerateV1Schemas_WithEmptyPath_ShouldThrow()
    {
        Action act = () => _generator.GenerateV1Schemas("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GenerateV2Schemas_WithEmptyPath_ShouldThrow()
    {
        Action act = () => _generator.GenerateV2Schemas("");

        act.Should().Throw<ArgumentException>();
    }
}
