using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Metadata;

namespace Synaxis.Contracts;

/// <summary>
/// Generates JSON schemas for Synaxis contracts.
/// </summary>
public class SchemaGenerator
{
    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaGenerator"/> class.
    /// </summary>
    public SchemaGenerator()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };
    }

    /// <summary>
    /// Generates a JSON schema for the specified type.
    /// </summary>
    /// <typeparam name="T">The type to generate a schema for.</typeparam>
    /// <returns>The JSON schema as a string.</returns>
    public string GenerateSchema<T>()
    {
        var schema = JsonSchemaExporter.GetJsonSchemaAsNode(_options, typeof(T));
        return schema.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Generates a JSON schema for the specified type.
    /// </summary>
    /// <param name="type">The type to generate a schema for.</param>
    /// <returns>The JSON schema as a string.</returns>
    public string GenerateSchema(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        var schema = JsonSchemaExporter.GetJsonSchemaAsNode(_options, type);
        return schema.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Generates JSON schemas for all V1 contracts and saves them to the specified directory.
    /// </summary>
    /// <param name="outputDirectory">The directory to save the schemas to.</param>
    /// <returns>A dictionary of type names to schema file paths.</returns>
    public Dictionary<string, string> GenerateV1Schemas(string outputDirectory)
    {
        ArgumentException.ThrowIfNullOrEmpty(outputDirectory);

        Directory.CreateDirectory(outputDirectory);

        var schemas = new Dictionary<string, string>();

        // Generate schemas for V1 contracts
        var v1Types = GetV1Types();

        foreach (var type in v1Types)
        {
            var schema = GenerateSchema(type);
            var fileName = $"{type.Name.ToLowerInvariant()}.schema.json";
            var filePath = Path.Combine(outputDirectory, fileName);

            File.WriteAllText(filePath, schema);
            schemas[type.Name] = filePath;
        }

        return schemas;
    }

    /// <summary>
    /// Generates JSON schemas for all V2 contracts and saves them to the specified directory.
    /// </summary>
    /// <param name="outputDirectory">The directory to save the schemas to.</param>
    /// <returns>A dictionary of type names to schema file paths.</returns>
    public Dictionary<string, string> GenerateV2Schemas(string outputDirectory)
    {
        ArgumentException.ThrowIfNullOrEmpty(outputDirectory);

        Directory.CreateDirectory(outputDirectory);

        var schemas = new Dictionary<string, string>();

        // Generate schemas for V2 contracts
        var v2Types = GetV2Types();

        foreach (var type in v2Types)
        {
            var schema = GenerateSchema(type);
            var fileName = $"{type.Name.ToLowerInvariant()}.schema.json";
            var filePath = Path.Combine(outputDirectory, fileName);

            File.WriteAllText(filePath, schema);
            schemas[type.Name] = filePath;
        }

        return schemas;
    }

    /// <summary>
    /// Gets all V1 contract types.
    /// </summary>
    /// <returns>A collection of V1 contract types.</returns>
    private static IEnumerable<Type> GetV1Types()
    {
        var types = new List<Type>();

        // Domain Events
        types.Add(typeof(V1.DomainEvents.UserCreated));
        types.Add(typeof(V1.DomainEvents.UserUpdated));
        types.Add(typeof(V1.DomainEvents.UserDeleted));
        types.Add(typeof(V1.DomainEvents.AgentCreated));
        types.Add(typeof(V1.DomainEvents.AgentExecutionStarted));
        types.Add(typeof(V1.DomainEvents.AgentExecutionCompleted));
        types.Add(typeof(V1.DomainEvents.WorkflowCreated));
        types.Add(typeof(V1.DomainEvents.WorkflowStepCompleted));
        types.Add(typeof(V1.DomainEvents.WorkflowFailed));

        // Commands
        types.Add(typeof(V1.Commands.CreateUserCommand));
        types.Add(typeof(V1.Commands.UpdateUserCommand));
        types.Add(typeof(V1.Commands.DeleteUserCommand));
        types.Add(typeof(V1.Commands.CreateAgentCommand));
        types.Add(typeof(V1.Commands.UpdateAgentCommand));
        types.Add(typeof(V1.Commands.DeleteAgentCommand));
        types.Add(typeof(V1.Commands.ExecuteAgentCommand));
        types.Add(typeof(V1.Commands.CancelAgentExecutionCommand));

        // Queries
        types.Add(typeof(V1.Queries.GetUserByIdQuery));
        types.Add(typeof(V1.Queries.GetUsersQuery));
        types.Add(typeof(V1.Queries.GetAgentByIdQuery));
        types.Add(typeof(V1.Queries.GetAgentsQuery));
        types.Add(typeof(V1.Queries.GetExecutionByIdQuery));
        types.Add(typeof(V1.Queries.GetExecutionsQuery));

        // DTOs
        types.Add(typeof(V1.DTOs.UserDto));
        types.Add(typeof(V1.DTOs.AgentDto));
        types.Add(typeof(V1.DTOs.ExecutionDto));
        types.Add(typeof(V1.DTOs.WorkflowDto));
        types.Add(typeof(V1.DTOs.ErrorResponse));

        // Enums
        types.Add(typeof(V1.Common.UserStatus));
        types.Add(typeof(V1.Common.AgentStatus));
        types.Add(typeof(V1.Common.ExecutionStatus));
        types.Add(typeof(V1.Common.WorkflowStatus));
        types.Add(typeof(V1.Common.EventType));

        return types;
    }

    /// <summary>
    /// Gets all V2 contract types.
    /// </summary>
    /// <returns>A collection of V2 contract types.</returns>
    private static IEnumerable<Type> GetV2Types()
    {
        var types = new List<Type>();

        // Domain Events
        types.Add(typeof(V2.DomainEvents.UserCreated));
        types.Add(typeof(V2.DomainEvents.UserUpdated));
        types.Add(typeof(V2.DomainEvents.UserDeleted));
        types.Add(typeof(V2.DomainEvents.AgentCreated));
        types.Add(typeof(V2.DomainEvents.AgentExecutionStarted));
        types.Add(typeof(V2.DomainEvents.AgentExecutionCompleted));
        types.Add(typeof(V2.DomainEvents.WorkflowCreated));
        types.Add(typeof(V2.DomainEvents.WorkflowStepCompleted));
        types.Add(typeof(V2.DomainEvents.WorkflowFailed));

        // Commands
        types.Add(typeof(V2.Commands.CreateUserCommand));
        types.Add(typeof(V2.Commands.UpdateUserCommand));
        types.Add(typeof(V2.Commands.DeleteUserCommand));
        types.Add(typeof(V2.Commands.CreateAgentCommand));
        types.Add(typeof(V2.Commands.UpdateAgentCommand));
        types.Add(typeof(V2.Commands.DeleteAgentCommand));
        types.Add(typeof(V2.Commands.ExecuteAgentCommand));
        types.Add(typeof(V2.Commands.CancelAgentExecutionCommand));

        // Queries
        types.Add(typeof(V2.Queries.GetUserByIdQuery));
        types.Add(typeof(V2.Queries.GetUsersQuery));
        types.Add(typeof(V2.Queries.GetAgentByIdQuery));
        types.Add(typeof(V2.Queries.GetAgentsQuery));
        types.Add(typeof(V2.Queries.GetExecutionByIdQuery));
        types.Add(typeof(V2.Queries.GetExecutionsQuery));

        // DTOs
        types.Add(typeof(V2.DTOs.UserDto));
        types.Add(typeof(V2.DTOs.AgentDto));
        types.Add(typeof(V2.DTOs.ExecutionDto));
        types.Add(typeof(V2.DTOs.WorkflowDto));
        types.Add(typeof(V2.DTOs.ErrorResponse));

        // Enums
        types.Add(typeof(V2.Common.UserStatus));
        types.Add(typeof(V2.Common.AgentStatus));
        types.Add(typeof(V2.Common.ExecutionStatus));
        types.Add(typeof(V2.Common.WorkflowStatus));
        types.Add(typeof(V2.Common.EventType));

        return types;
    }
}
