// <copyright file="AgentConverter.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Contracts.V2.Converters;

using Synaxis.Shared.Contracts.V2.DTOs;

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
                LastExecutedAt = null,
            },
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
            _ => Common.AgentStatus.Provisioning,
        };
    }

    private static IReadOnlyDictionary<string, string>? ConvertTagsToLabels(IReadOnlyList<string> tags)
    {
        if (tags.Count == 0)
        {
            return null;
        }

        // Convert simple tags to key-value labels (tag name = "true")
        return tags.ToDictionary(
            tag => tag,
            _ => "true",
            StringComparer.OrdinalIgnoreCase);
    }
}
