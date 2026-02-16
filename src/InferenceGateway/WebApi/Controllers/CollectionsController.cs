// <copyright file="CollectionsController.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Security.Claims;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Cors;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Synaxis.Core.Models;
    using Synaxis.InferenceGateway.Application.Security;
    using Synaxis.Infrastructure.Data;

    /// <summary>
    /// Controller for managing collections within organizations.
    /// </summary>
    [ApiController]
    [Route("api/v1/organizations/{orgId}/collections")]
    [Authorize]
    [EnableCors("WebApp")]
    public class CollectionsController : ControllerBase
    {
        private readonly SynaxisDbContext _dbContext;
        private readonly IAuditService _auditService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionsController"/> class.
        /// </summary>
        /// <param name="dbContext">The database context.</param>
        /// <param name="auditService">The audit service.</param>
        public CollectionsController(SynaxisDbContext dbContext, IAuditService auditService)
        {
            this._dbContext = dbContext;
            this._auditService = auditService;
        }

        /// <summary>
        /// Creates a new collection in an organization.
        /// </summary>
        /// <param name="orgId">The organization ID.</param>
        /// <param name="request">The create collection request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created collection.</returns>
        [HttpPost]
        public async Task<IActionResult> CreateCollection(
            Guid orgId,
            [FromBody] CreateCollectionRequest request,
            CancellationToken cancellationToken)
        {
            var userId = this.GetUserId();

            var validationResult = await this.ValidateCollectionCreationAsync(userId, orgId, request, cancellationToken).ConfigureAwait(false);
            if (validationResult != null)
            {
                return validationResult;
            }

            var collection = this.CreateCollectionEntity(orgId, userId, request);
            this._dbContext.Collections.Add(collection);

            await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // Add creator as admin member
            var membership = this.CreateCollectionMembership(userId, collection.Id, orgId, "Admin");
            this._dbContext.CollectionMemberships.Add(membership);

            await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await this._auditService.LogAsync(
                orgId,
                userId,
                "CreateCollection",
                new { CollectionId = collection.Id, Name = collection.Name },
                cancellationToken).ConfigureAwait(false);

            return this.CreatedAtAction(
                nameof(this.GetCollection),
                new { orgId, id = collection.Id },
                new
                {
                    id = collection.Id,
                    name = collection.Name,
                    description = collection.Description,
                    slug = collection.Slug,
                    type = collection.Type,
                    visibility = collection.Visibility,
                    isActive = collection.IsActive,
                    createdAt = collection.CreatedAt,
                });
        }

        /// <summary>
        /// Lists collections in an organization.
        /// </summary>
        /// <param name="orgId">The organization ID.</param>
        /// <param name="teamId">Optional team ID filter.</param>
        /// <param name="type">Optional type filter.</param>
        /// <param name="visibility">Optional visibility filter.</param>
        /// <param name="page">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A paginated list of collections.</returns>
        [HttpGet]
        public async Task<IActionResult> ListCollections(
            Guid orgId,
            [FromQuery] Guid? teamId,
            [FromQuery] string? type,
            [FromQuery] string? visibility,
            [FromQuery] int page = 0,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var userId = this.GetUserId();

            var isMember = await this._dbContext.Users
                .AnyAsync(u => u.Id == userId && u.OrganizationId == orgId, cancellationToken)
                .ConfigureAwait(false);

            if (!isMember)
            {
                return this.Forbid();
            }

            var query = this.BuildCollectionsQuery(orgId, teamId, type, visibility);
            var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);

            var collections = await query
                .OrderBy(c => c.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(c => c.CollectionMemberships)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var result = collections.Select(c => new
            {
                id = c.Id,
                name = c.Name,
                description = c.Description,
                slug = c.Slug,
                type = c.Type,
                visibility = c.Visibility,
                teamId = c.TeamId,
                isActive = c.IsActive,
                memberCount = c.CollectionMemberships?.Count ?? 0,
                createdAt = c.CreatedAt,
                updatedAt = c.UpdatedAt,
            }).ToList();

            return this.Ok(new
            {
                items = result,
                page,
                pageSize,
                total,
                totalPages = (int)Math.Ceiling((double)total / pageSize),
            });
        }

        private IQueryable<Collection> BuildCollectionsQuery(Guid orgId, Guid? teamId, string? type, string? visibility)
        {
            var query = this._dbContext.Collections.Where(c => c.OrganizationId == orgId);

            if (teamId.HasValue)
            {
                query = query.Where(c => c.TeamId == teamId.Value);
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(c => c.Type == type);
            }

            if (!string.IsNullOrWhiteSpace(visibility))
            {
                query = query.Where(c => c.Visibility == visibility);
            }

            return query;
        }

        /// <summary>
        /// Gets details of a specific collection.
        /// </summary>
        /// <param name="orgId">The organization ID.</param>
        /// <param name="id">The collection ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Collection details.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCollection(
            Guid orgId,
            Guid id,
            CancellationToken cancellationToken)
        {
            var userId = this.GetUserId();

            // Check if user has access to the collection
            var hasAccess = await this._dbContext.CollectionMemberships
                .AnyAsync(cm => cm.CollectionId == id && cm.UserId == userId && cm.OrganizationId == orgId, cancellationToken)
                .ConfigureAwait(false);

            if (!hasAccess)
            {
                return this.Forbid();
            }

            var collectionEntity = await this._dbContext.Collections
                .Include(c => c.CollectionMemberships)
                .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == orgId, cancellationToken)
                .ConfigureAwait(false);

            if (collectionEntity == null)
            {
                return this.NotFound();
            }

            var collection = new
            {
                id = collectionEntity.Id,
                name = collectionEntity.Name,
                description = collectionEntity.Description,
                slug = collectionEntity.Slug,
                type = collectionEntity.Type,
                visibility = collectionEntity.Visibility,
                teamId = collectionEntity.TeamId,
                isActive = collectionEntity.IsActive,
                tags = collectionEntity.Tags,
                metadata = collectionEntity.Metadata,
                memberCount = collectionEntity.CollectionMemberships?.Count ?? 0,
                createdAt = collectionEntity.CreatedAt,
                updatedAt = collectionEntity.UpdatedAt,
                createdBy = collectionEntity.CreatedBy,
            };

            if (collection == null)
            {
                return this.NotFound("Collection not found");
            }

            return this.Ok(collection);
        }

        /// <summary>
        /// Updates a collection.
        /// </summary>
        /// <param name="orgId">The organization ID.</param>
        /// <param name="id">The collection ID.</param>
        /// <param name="request">The update request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Updated collection details.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCollection(
            Guid orgId,
            Guid id,
            [FromBody] UpdateCollectionRequest request,
            CancellationToken cancellationToken)
        {
            var userId = this.GetUserId();

            var hasPermission = await this.CheckCollectionAdminPermissionAsync(userId, orgId, id, cancellationToken).ConfigureAwait(false);
            if (!hasPermission)
            {
                return this.Forbid();
            }

            var collection = await this._dbContext.Collections
                .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == orgId, cancellationToken)
                .ConfigureAwait(false);

            if (collection == null)
            {
                return this.NotFound("Collection not found");
            }

            ApplyCollectionUpdates(collection, request);

            await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await this._auditService.LogAsync(
                orgId,
                userId,
                "UpdateCollection",
                new { CollectionId = collection.Id, Name = collection.Name },
                cancellationToken).ConfigureAwait(false);

            return this.Ok(new
            {
                id = collection.Id,
                name = collection.Name,
                description = collection.Description,
                slug = collection.Slug,
                type = collection.Type,
                visibility = collection.Visibility,
                isActive = collection.IsActive,
                tags = collection.Tags,
                updatedAt = collection.UpdatedAt,
            });
        }

        /// <summary>
        /// Deletes a collection.
        /// </summary>
        /// <param name="orgId">The organization ID.</param>
        /// <param name="id">The collection ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>No content.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCollection(
            Guid orgId,
            Guid id,
            CancellationToken cancellationToken)
        {
            var userId = this.GetUserId();

            var hasPermission = await this.CheckCollectionAdminPermissionAsync(userId, orgId, id, cancellationToken).ConfigureAwait(false);
            if (!hasPermission)
            {
                return this.Forbid();
            }

            var collection = await this._dbContext.Collections
                .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == orgId, cancellationToken)
                .ConfigureAwait(false);

            if (collection == null)
            {
                return this.NotFound("Collection not found");
            }

            this._dbContext.Collections.Remove(collection);
            await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await this._auditService.LogAsync(
                orgId,
                userId,
                "DeleteCollection",
                new { CollectionId = collection.Id, Name = collection.Name },
                cancellationToken).ConfigureAwait(false);

            return this.NoContent();
        }

        /// <summary>
        /// Lists members of a collection.
        /// </summary>
        /// <param name="orgId">The organization ID.</param>
        /// <param name="id">The collection ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>List of collection members.</returns>
        [HttpGet("{id}/members")]
        public async Task<IActionResult> ListMembers(
            Guid orgId,
            Guid id,
            CancellationToken cancellationToken)
        {
            var userId = this.GetUserId();

            // Check if user has access to the collection
            var hasAccess = await this._dbContext.CollectionMemberships
                .AnyAsync(cm => cm.CollectionId == id && cm.UserId == userId && cm.OrganizationId == orgId, cancellationToken)
                .ConfigureAwait(false);

            if (!hasAccess)
            {
                return this.Forbid();
            }

            var members = await this._dbContext.CollectionMemberships
                .Where(cm => cm.CollectionId == id && cm.OrganizationId == orgId)
                .Select(cm => new
                {
                    userId = cm.UserId,
                    role = cm.Role,
                    joinedAt = cm.JoinedAt,
                    addedBy = cm.AddedBy,
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return this.Ok(new { members });
        }

        /// <summary>
        /// Adds a member to a collection.
        /// </summary>
        /// <param name="orgId">The organization ID.</param>
        /// <param name="id">The collection ID.</param>
        /// <param name="request">The add member request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Created response.</returns>
        [HttpPost("{id}/members")]
        public async Task<IActionResult> AddMember(
            Guid orgId,
            Guid id,
            [FromBody] AddCollectionMemberRequest request,
            CancellationToken cancellationToken)
        {
            var userId = this.GetUserId();

            var hasPermission = await this.CheckCollectionAdminPermissionAsync(userId, orgId, id, cancellationToken).ConfigureAwait(false);
            if (!hasPermission)
            {
                return this.Forbid();
            }

            var validationResult = await this.ValidateAddMemberRequestAsync(orgId, id, request, cancellationToken).ConfigureAwait(false);
            if (validationResult != null)
            {
                return validationResult;
            }

            var validRole = ValidateRole(request.Role)!;

            var membership = new CollectionMembership
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                CollectionId = id,
                OrganizationId = orgId,
                Role = validRole,
                JoinedAt = DateTime.UtcNow,
                AddedBy = userId,
            };

            this._dbContext.CollectionMemberships.Add(membership);
            await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await this._auditService.LogAsync(
                orgId,
                userId,
                "AddCollectionMember",
                new { CollectionId = id, MemberId = request.UserId, Role = validRole },
                cancellationToken).ConfigureAwait(false);

            return this.StatusCode(201);
        }

        /// <summary>
        /// Removes a member from a collection.
        /// </summary>
        /// <param name="orgId">The organization ID.</param>
        /// <param name="id">The collection ID.</param>
        /// <param name="userId">The user ID to remove.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>No content response.</returns>
        [HttpDelete("{id}/members/{userId}")]
        public async Task<IActionResult> RemoveMember(
            Guid orgId,
            Guid id,
            Guid userId,
            CancellationToken cancellationToken)
        {
            var currentUserId = this.GetUserId();

            var isSelfRemoval = currentUserId == userId;
            if (!isSelfRemoval)
            {
                var hasPermission = await this.CheckCollectionAdminPermissionAsync(currentUserId, orgId, id, cancellationToken).ConfigureAwait(false);
                if (!hasPermission)
                {
                    return this.Forbid();
                }
            }

            var collection = await this._dbContext.Collections
                .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == orgId, cancellationToken)
                .ConfigureAwait(false);

            if (collection == null)
            {
                return this.NotFound("Collection not found");
            }

            var membership = await this._dbContext.CollectionMemberships
                .FirstOrDefaultAsync(m => m.UserId == userId && m.CollectionId == id, cancellationToken)
                .ConfigureAwait(false);

            if (membership == null)
            {
                return this.NotFound("Member not found in collection");
            }

            this._dbContext.CollectionMemberships.Remove(membership);
            await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await this._auditService.LogAsync(
                orgId,
                currentUserId,
                "RemoveCollectionMember",
                new { CollectionId = id, MemberId = userId },
                cancellationToken).ConfigureAwait(false);

            return this.NoContent();
        }

        private Guid GetUserId()
        {
            var userIdClaim = this.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? this.User.FindFirstValue("sub");
            return Guid.Parse(userIdClaim!);
        }

        private async Task<IActionResult?> ValidateCollectionCreationAsync(
            Guid userId,
            Guid orgId,
            CreateCollectionRequest request,
            CancellationToken cancellationToken)
        {
            var user = await this._dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.OrganizationId == orgId, cancellationToken)
                .ConfigureAwait(false);

            if (user == null)
            {
                return this.Forbid();
            }

            var org = await this._dbContext.Organizations
                .FirstOrDefaultAsync(o => o.Id == orgId, cancellationToken)
                .ConfigureAwait(false);

            if (org == null)
            {
                return this.NotFound("Organization not found");
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return this.BadRequest("Name is required");
            }

            var validType = ValidateType(request.Type);
            if (validType == null)
            {
                return this.BadRequest("Type must be one of: general, models, prompts, datasets, workflows");
            }

            var validVisibility = ValidateVisibility(request.Visibility);
            if (validVisibility == null)
            {
                return this.BadRequest("Visibility must be one of: public, private, team");
            }

            if (request.TeamId.HasValue)
            {
                var team = await this._dbContext.Teams
                    .FirstOrDefaultAsync(t => t.Id == request.TeamId.Value && t.OrganizationId == orgId, cancellationToken)
                    .ConfigureAwait(false);

                if (team == null)
                {
                    return this.NotFound("Team not found");
                }
            }

            return null;
        }

        private Collection CreateCollectionEntity(Guid orgId, Guid userId, CreateCollectionRequest request)
        {
            return new Collection
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgId,
                TeamId = request.TeamId,
                Name = request.Name,
                Description = request.Description ?? string.Empty,
                Slug = GenerateSlug(request.Name),
                Type = ValidateType(request.Type)!,
                Visibility = ValidateVisibility(request.Visibility)!,
                IsActive = true,
                Tags = request.Tags?.ToList() ?? new List<string>(),
                Metadata = new Dictionary<string, object>(),
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
        }

        private CollectionMembership CreateCollectionMembership(Guid userId, Guid collectionId, Guid orgId, string role)
        {
            return new CollectionMembership
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CollectionId = collectionId,
                OrganizationId = orgId,
                Role = role,
                JoinedAt = DateTime.UtcNow,
            };
        }

        private async Task<IActionResult?> ValidateAddMemberRequestAsync(
            Guid orgId,
            Guid collectionId,
            AddCollectionMemberRequest request,
            CancellationToken cancellationToken)
        {
            var validRole = ValidateRole(request.Role);
            if (validRole == null)
            {
                return this.BadRequest("Role must be 'admin', 'member', or 'viewer'");
            }

            var collection = await this._dbContext.Collections
                .FirstOrDefaultAsync(c => c.Id == collectionId && c.OrganizationId == orgId, cancellationToken)
                .ConfigureAwait(false);

            if (collection == null)
            {
                return this.NotFound("Collection not found");
            }

            var userToAdd = await this._dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
                .ConfigureAwait(false);

            if (userToAdd == null)
            {
                return this.BadRequest("User not found");
            }

            var userInOrg = await this._dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId && u.OrganizationId == orgId, cancellationToken)
                .ConfigureAwait(false);

            if (userInOrg == null)
            {
                return this.BadRequest("User is not a member of the organization");
            }

            var existingMembership = await this._dbContext.CollectionMemberships
                .FirstOrDefaultAsync(m => m.UserId == request.UserId && m.CollectionId == collectionId, cancellationToken)
                .ConfigureAwait(false);

            if (existingMembership != null)
            {
                return this.BadRequest("User is already a member of the collection");
            }

            return null;
        }

        private async Task<bool> CheckCollectionAdminPermissionAsync(
            Guid userId,
            Guid orgId,
            Guid collectionId,
            CancellationToken cancellationToken)
        {
            var membership = await this._dbContext.CollectionMemberships
                .FirstOrDefaultAsync(cm => cm.UserId == userId && cm.CollectionId == collectionId && cm.OrganizationId == orgId, cancellationToken)
                .ConfigureAwait(false);

            var isAdmin = membership != null && string.Equals(membership.Role, "Admin", StringComparison.Ordinal);

            if (isAdmin)
            {
                return true;
            }

            var user = await this._dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.OrganizationId == orgId, cancellationToken)
                .ConfigureAwait(false);

            var isOrgAdmin = user != null && (string.Equals(user.Role, "admin", StringComparison.OrdinalIgnoreCase) || string.Equals(user.Role, "owner", StringComparison.OrdinalIgnoreCase));

            return isOrgAdmin;
        }

        private static void ApplyCollectionUpdates(Collection collection, UpdateCollectionRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                collection.Name = request.Name;
            }

            if (request.Description != null)
            {
                collection.Description = request.Description;
            }

            if (request.Type != null)
            {
                collection.Type = request.Type;
            }

            if (request.Visibility != null)
            {
                collection.Visibility = request.Visibility;
            }

            if (request.IsActive.HasValue)
            {
                collection.IsActive = request.IsActive.Value;
            }

            if (request.Tags != null)
            {
                collection.Tags = request.Tags.ToList();
            }

            collection.UpdatedAt = DateTime.UtcNow;
        }

        private static string? ValidateType(string type)
        {
            var normalized = type?.Trim().ToLowerInvariant();
            return normalized switch
            {
                "general" => "general",
                "models" => "models",
                "prompts" => "prompts",
                "datasets" => "datasets",
                "workflows" => "workflows",
                _ => null,
            };
        }

        private static string? ValidateVisibility(string visibility)
        {
            var normalized = visibility?.Trim().ToLowerInvariant();
            return normalized switch
            {
                "public" => "public",
                "private" => "private",
                "team" => "team",
                _ => null,
            };
        }

        private static string? ValidateRole(string role)
        {
            var normalized = role.Trim().ToLowerInvariant();
            return normalized switch
            {
                "admin" => "Admin",
                "member" => "Member",
                "viewer" => "Viewer",
                _ => null,
            };
        }

        private static string GenerateSlug(string name)
        {
            return name.ToLowerInvariant()
                .Replace(" ", "-")
                .Replace("_", "-")
                + "-" + Guid.NewGuid().ToString("N")[..6];
        }
    }

    /// <summary>
    /// Request to create a new collection.
    /// </summary>
    public class CreateCollectionRequest
    {
        /// <summary>
        /// Gets or sets the collection name.
        /// </summary>
        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the collection description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the team ID (optional - for team-specific collections).
        /// </summary>
        public Guid? TeamId { get; set; }

        /// <summary>
        /// Gets or sets the collection type.
        /// </summary>
        public string Type { get; set; } = "general";

        /// <summary>
        /// Gets or sets the collection visibility.
        /// </summary>
        public string Visibility { get; set; } = "private";

        /// <summary>
        /// Gets or sets the tags.
        /// </summary>
        public IEnumerable<string>? Tags { get; set; }
    }

    /// <summary>
    /// Request to update a collection.
    /// </summary>
    public class UpdateCollectionRequest
    {
        /// <summary>
        /// Gets or sets the collection name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the collection description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the collection type.
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Gets or sets the collection visibility.
        /// </summary>
        public string? Visibility { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the collection is active.
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// Gets or sets the tags.
        /// </summary>
        public IEnumerable<string>? Tags { get; set; }
    }

    /// <summary>
    /// Request to add a member to a collection.
    /// </summary>
    public class AddCollectionMemberRequest
    {
        /// <summary>
        /// Gets or sets the user ID to add.
        /// </summary>
        [Required]
        [JsonRequired]
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the role for the member.
        /// </summary>
        [Required]
        public string Role { get; set; } = "member";
    }
}
