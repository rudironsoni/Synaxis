// <copyright file="CreateTeamRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Contracts
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Request to create a new team.
    /// </summary>
    public class CreateTeamRequest
    {
        /// <summary>
        /// Gets or sets the team slug.
        /// </summary>
        public required string Slug { get; set; }

        /// <summary>
        /// Gets or sets the team name.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the team description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the monthly budget.
        /// </summary>
        public decimal? MonthlyBudget { get; set; }

        /// <summary>
        /// Gets or sets the allowed models.
        /// </summary>
        public IList<string> AllowedModels { get; set; } = new List<string>();
    }

    /// <summary>
    /// Request to update an existing team.
    /// </summary>
    public class UpdateTeamRequest
    {
        /// <summary>
        /// Gets or sets the team name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the team description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the monthly budget.
        /// </summary>
        public decimal? MonthlyBudget { get; set; }

        /// <summary>
        /// Gets or sets the allowed models.
        /// </summary>
        public IList<string> AllowedModels { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the blocked models.
        /// </summary>
        public IList<string> BlockedModels { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets a value indicating whether the team is active.
        /// </summary>
        public bool? IsActive { get; set; }
    }

    /// <summary>
    /// Team response DTO.
    /// </summary>
    public class TeamResponse
    {
        /// <summary>
        /// Gets or sets the team ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the organization ID.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the team slug.
        /// </summary>
        public required string Slug { get; set; }

        /// <summary>
        /// Gets or sets the team name.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the team description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the team is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the monthly budget.
        /// </summary>
        public decimal? MonthlyBudget { get; set; }

        /// <summary>
        /// Gets or sets the member count.
        /// </summary>
        public int MemberCount { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the last update timestamp.
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Paginated list of teams.
    /// </summary>
    public class TeamListResponse
    {
        /// <summary>
        /// Gets or sets the list of teams.
        /// </summary>
        public required IList<TeamResponse> Teams { get; set; }

        /// <summary>
        /// Gets or sets the total count.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Gets or sets the page number.
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Gets or sets the page size.
        /// </summary>
        public int PageSize { get; set; }
    }

    /// <summary>
    /// Team statistics response DTO.
    /// </summary>
    public class TeamStatsResponse
    {
        /// <summary>
        /// Gets or sets the team ID.
        /// </summary>
        public Guid TeamId { get; set; }

        /// <summary>
        /// Gets or sets the member count.
        /// </summary>
        public int MemberCount { get; set; }

        /// <summary>
        /// Gets or sets the active virtual key count.
        /// </summary>
        public int ActiveKeyCount { get; set; }

        /// <summary>
        /// Gets or sets the total request count for the current month.
        /// </summary>
        public int MonthlyRequestCount { get; set; }

        /// <summary>
        /// Gets or sets the total cost for the current month.
        /// </summary>
        public decimal MonthlyCost { get; set; }

        /// <summary>
        /// Gets or sets the monthly budget.
        /// </summary>
        public decimal? MonthlyBudget { get; set; }

        /// <summary>
        /// Gets or sets the budget utilization percentage.
        /// </summary>
        public decimal? BudgetUtilization { get; set; }
    }
}
