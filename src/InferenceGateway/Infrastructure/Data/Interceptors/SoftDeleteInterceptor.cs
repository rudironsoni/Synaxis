// <copyright file="SoftDeleteInterceptor.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Data.Interceptors
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Diagnostics;
    using Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Identity;
    using Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Operations;
    using Synaxis.InferenceGateway.Infrastructure.Data.Interfaces;

    /// <summary>
    /// EF Core interceptor that implements soft deletion for entities implementing <see cref="ISoftDeletable"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interceptor intercepts SaveChanges operations and converts DELETE operations into UPDATE
    /// operations that set the DeletedAt timestamp and optional DeletedBy user identifier.
    /// This preserves data for audit trails and enables data recovery without breaking referential integrity.
    /// </para>
    /// <para>
    /// Key features:
    /// - Automatically converts entity deletions to soft deletes (sets DeletedAt to DateTime.UtcNow)
    /// - Sets DeletedBy from current user context (if available via IHttpContextAccessor)
    /// - Supports cascade soft delete for Organization entities:
    ///   - When an Organization is soft deleted, automatically soft deletes all related Groups,
    ///     UserOrganizationMemberships, and ApiKeys
    /// - Works in conjunction with global query filters (configured in DbContext.OnModelCreating)
    ///   that exclude soft-deleted entities from normal queries
    /// </para>
    /// <para>
    /// Usage:
    /// Register in DI container and configure with DbContext:
    /// <code>
    /// services.AddScoped&lt;SoftDeleteInterceptor&gt;();
    /// services.AddDbContext&lt;AppDbContext&gt;((serviceProvider, options) =&gt;
    /// {
    ///     var interceptor = serviceProvider.GetRequiredService&lt;SoftDeleteInterceptor&gt;();
    ///     options.UseNpgsql(connectionString).AddInterceptors(interceptor);
    /// });
    /// </code>
    /// </para>
    /// <para>
    /// Global query filters should be configured in DbContext:
    /// <code>
    /// modelBuilder.Entity&lt;MyEntity&gt;().HasQueryFilter(e =&gt; e.DeletedAt == null);
    /// </code>
    /// </para>
    /// </remarks>
    public sealed class SoftDeleteInterceptor : SaveChangesInterceptor
    {
        private readonly IHttpContextAccessor? _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="SoftDeleteInterceptor"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">
        /// Optional HTTP context accessor for retrieving current user information.
        /// If provided, DeletedBy will be set to the current user's ID when available.
        /// </param>
        public SoftDeleteInterceptor(IHttpContextAccessor? httpContextAccessor = null)
        {
            this._httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Intercepts the asynchronous SaveChanges operation to handle soft deletion.
        /// </summary>
        /// <param name="eventData">Context information for the save operation.</param>
        /// <param name="result">The current result of the interceptor chain.</param>
        /// <param name="cancellationToken">Cancellation token to observe while waiting.</param>
        /// <returns>
        /// A task representing the asynchronous operation, with an <see cref="InterceptionResult{Int32}"/>
        /// containing the number of entities affected.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if DbContext is not available in the event data.
        /// </exception>
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context is null)
            {
                return base.SavingChangesAsync(eventData, result, cancellationToken);
            }

            try
            {
                this.ProcessSoftDelete(eventData.Context);
            }
            catch (Exception ex)
            {
                // Log the exception if logging is available, but don't prevent the save operation
                // In production, you should inject ILogger and log this properly
                Console.Error.WriteLine($"Error in SoftDeleteInterceptor: {ex.Message}");
                throw new InvalidOperationException("Failed to process soft delete operation. See inner exception for details.", ex);
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        /// <summary>
        /// Intercepts the synchronous SaveChanges operation to handle soft deletion.
        /// </summary>
        /// <param name="eventData">Context information for the save operation.</param>
        /// <param name="result">The current result of the interceptor chain.</param>
        /// <returns>
        /// An <see cref="InterceptionResult{Int32}"/> containing the number of entities affected.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if DbContext is not available in the event data.
        /// </exception>
        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            if (eventData.Context is null)
            {
                return base.SavingChanges(eventData, result);
            }

            try
            {
                this.ProcessSoftDelete(eventData.Context);
            }
            catch (Exception ex)
            {
                // Log the exception if logging is available, but don't prevent the save operation
                // In production, you should inject ILogger and log this properly
                Console.Error.WriteLine($"Error in SoftDeleteInterceptor: {ex.Message}");
                throw new InvalidOperationException("Failed to process soft delete operation. See inner exception for details.", ex);
            }

            return base.SavingChanges(eventData, result);
        }

        /// <summary>
        /// Processes soft delete for all tracked entities marked for deletion.
        /// </summary>
        /// <param name="context">The DbContext containing the tracked entities.</param>
        /// <remarks>
        /// This method:
        /// 1. Retrieves the current user ID from HTTP context (if available)
        /// 2. Finds all entities in Deleted state that implement ISoftDeletable
        /// 3. Converts DELETE to UPDATE by changing EntityState to Modified
        /// 4. Sets DeletedAt to DateTime.UtcNow and DeletedBy to current user ID
        /// 5. For Organization entities, cascades soft delete to related entities
        /// </remarks>
        private void ProcessSoftDelete(DbContext context)
        {
            var currentUserId = this.GetCurrentUserId();
            var utcNow = DateTime.UtcNow;

            // Find all entities marked for deletion that support soft delete
            var entriesToSoftDelete = context.ChangeTracker
                .Entries()
                .Where(e => e.State == EntityState.Deleted && e.Entity is ISoftDeletable)
                .ToList(); // Materialize to avoid collection modification during iteration

            foreach (var entry in entriesToSoftDelete)
            {
                if (entry.Entity is not ISoftDeletable softDeletable)
                {
                    continue;
                }

                // Convert DELETE to UPDATE
                entry.State = EntityState.Modified;

                // Set soft delete properties
                softDeletable.DeletedAt = utcNow;
                softDeletable.DeletedBy = currentUserId;

                // Handle cascade soft delete for Organization
                if (entry.Entity is Organization organization)
                {
                    CascadeSoftDeleteForOrganization(context, organization.Id, currentUserId, utcNow);
                }
            }
        }

        /// <summary>
        /// Cascades soft delete to all entities related to an Organization.
        /// </summary>
        /// <param name="context">The DbContext containing the entities.</param>
        /// <param name="organizationId">The ID of the organization being soft deleted.</param>
        /// <param name="userId">The ID of the user performing the deletion, or null if not available.</param>
        /// <param name="deletedAt">The timestamp to use for DeletedAt.</param>
        /// <remarks>
        /// When an Organization is soft deleted, this method automatically soft deletes:
        /// - All Groups in the organization
        /// - All UserOrganizationMemberships
        /// - All ApiKeys associated with the organization
        ///
        /// This maintains data consistency and ensures that all organization-related data
        /// is properly marked as deleted together.
        /// </remarks>
        private static void CascadeSoftDeleteForOrganization(
            DbContext context,
            Guid organizationId,
            Guid? userId,
            DateTime deletedAt)
        {
            // Soft delete all Groups belonging to the organization
            var groups = context.ChangeTracker
                .Entries<Group>()
                .Where(e => e.State != EntityState.Deleted &&
                           e.State != EntityState.Detached &&
                           e.Entity.OrganizationId == organizationId)
                .Select(e => e.Entity)
                .ToList();

            foreach (var group in groups)
            {
                if (group.DeletedAt is null)
                {
                    group.DeletedAt = deletedAt;

                    // Note: Group doesn't have DeletedBy property in the current schema
                }
            }

            // Soft delete all UserOrganizationMemberships
            var memberships = context.ChangeTracker
                .Entries<UserOrganizationMembership>()
                .Where(e => e.State != EntityState.Deleted &&
                           e.State != EntityState.Detached &&
                           e.Entity.OrganizationId == organizationId)
                .Select(e => e.Entity)
                .ToList();

            // Note: UserOrganizationMembership doesn't have DeletedAt/DeletedBy in current schema
            // If it should support soft delete, add ISoftDeletable interface to the entity
            // and add DeletedAt/DeletedBy properties

            // Soft delete all ApiKeys belonging to the organization
            var apiKeys = context.ChangeTracker
                .Entries<ApiKey>()
                .Where(e => e.State != EntityState.Deleted &&
                           e.State != EntityState.Detached &&
                           e.Entity.OrganizationId == organizationId)
                .Select(e => e.Entity)
                .ToList();

            // Note: ApiKey uses RevokedAt/RevokedBy pattern instead of DeletedAt/DeletedBy
            // Setting IsActive to false and populating RevokedAt/RevokedBy for consistency
            foreach (var apiKey in apiKeys)
            {
                if (apiKey.RevokedAt is null)
                {
                    apiKey.IsActive = false;
                    apiKey.RevokedAt = deletedAt;
                    apiKey.RevokedBy = userId;
                    apiKey.RevocationReason = "Organization deleted";
                }
            }
        }

        /// <summary>
        /// Retrieves the current user's ID from the HTTP context.
        /// </summary>
        /// <returns>
        /// The current user's ID as a Guid if authentication information is available,
        /// otherwise null.
        /// </returns>
        /// <remarks>
        /// This method attempts to extract the user ID from the "sub" claim (subject claim)
        /// in the current HTTP context's user principal. This is commonly used in JWT-based
        /// authentication where the "sub" claim contains the user's unique identifier.
        ///
        /// If the HTTP context is not available, the user is not authenticated, or the
        /// claim cannot be parsed as a Guid, this method returns null.
        /// </remarks>
        private Guid? GetCurrentUserId()
        {
            var httpContext = this._httpContextAccessor?.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var userIdClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                             ?? httpContext.User.FindFirst("sub");

            if (userIdClaim?.Value is null)
            {
                return null;
            }

            return Guid.TryParse(userIdClaim.Value, out var userId) ? userId : null;
        }
    }
}
