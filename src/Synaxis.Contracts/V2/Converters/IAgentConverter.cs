using Synaxis.Contracts.V2.DTOs;

namespace Synaxis.Contracts.V2.Converters;

/// <summary>
/// Converts V1 AgentDto to V2 AgentDto.
/// </summary>
public interface IAgentConverter
{
    /// <summary>
    /// Converts a V1 AgentDto to V2 AgentDto.
    /// </summary>
    /// <param name="v1Agent">The V1 agent DTO.</param>
    /// <returns>The V2 agent DTO.</returns>
    AgentDto Convert(V1.DTOs.AgentDto v1Agent);
}

/// <summary>
/// Default implementation of the agent converter.
/// </summary>
public class AgentConverter : IAgentConverter
{
    /// <inheritdoc />
    public AgentDto Convert(V1.DTOs.AgentDto v1Agent)
    {
        ArgumentNullException.ThrowIfNull(v1Agent);

        return new AgentDto
        {
            Id = v1Agent.Id,
            Name = v1Agent.Name,
            Description = v1Agent.Description,
            AgentType = v1Agent.AgentType,
            Status = ConvertAgentStatus(v1Agent.Status),
            Configuration = v1Agent.Configuration,
            Resources = null,
            Labels = ConvertTagsToLabels(v1Agent.Tags),
            CreatedByUserId = v1Agent.CreatedByUserId,
            CreatedAt = v1Agent.CreatedAt,
            UpdatedAt = v1Agent.UpdatedAt,
            Stats = new ExecutionStats
            {
                TotalCount = v1Agent.ExecutionCount,
                SuccessCount = 0,
                FailureCount = 0,
                AverageDuration = null,
                LastExecutedAt = null
            }
        };
    }

    private static Common.AgentStatus ConvertAgentStatus(V1.Common.AgentStatus status)
    {
        return status switch
        {
            V1.Common.AgentStatus.Creating => Common.AgentStatus.Provisioning,
            V1.Common.AgentStatus.Idle => Common.AgentStatus.Idle,
            V1.Common.AgentStatus.Executing => Common.AgentStatus.Processing,
            V1.Common.AgentStatus.Error => Common.AgentStatus.Error,
            V1.Common.AgentStatus.Disabled => Common.AgentStatus.Disabled,
            _ => Common.AgentStatus.Provisioning
        };
    }

    private static Dictionary<string, string>? ConvertTagsToLabels(IReadOnlyList<string> tags)
    {
        if (tags.Count == 0) return null;

        // Convert simple tags to key-value labels (tag name = "true")
        return tags.ToDictionary(
            tag => tag,
            _ => "true",
            StringComparer.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Extension methods for agent conversion.
/// </summary>
public static class AgentConverterExtensions
{
    /// <summary>
    /// Converts a V1 AgentDto to V2 AgentDto.
    /// </summary>
    /// <param name="v1Agent">The V1 agent DTO.</param>
    /// <returns>The V2 agent DTO.</returns>
    public static AgentDto ToV2(this V1.DTOs.AgentDto v1Agent)
    {
        return new AgentConverter().Convert(v1Agent);
    }

    /// <summary>
    /// Converts a collection of V1 AgentDto to V2 AgentDto.
    /// </summary>
    /// <param name="v1Agents">The V1 agent DTOs.</param>
    /// <returns>The V2 agent DTOs.</returns>
    public static IEnumerable<AgentDto> ToV2(this IEnumerable<V1.DTOs.AgentDto> v1Agents)
    {
        var converter = new AgentConverter();
        return v1Agents.Select(converter.Convert);
    }
}
