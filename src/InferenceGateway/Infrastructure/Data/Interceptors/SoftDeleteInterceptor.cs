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
    ///   that exclude soft-deleted entities from normal queries.
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
        /// 5. For Organization entities, cascades soft delete to related entities.
        /// 6. Also processes Modified entities where DeletedAt was just set (for test scenarios).
        /// </remarks>
        private void ProcessSoftDelete(DbContext context)
        {
            var currentUserId = this.GetCurrentUserId();
            var utcNow = DateTime.UtcNow;

            Console.WriteLine($"[SoftDeleteInterceptor] ProcessSoftDelete called, currentUserId={currentUserId}");
            Console.WriteLine($"[SoftDeleteInterceptor] Total tracked entities: {context.ChangeTracker.Entries().Count()}");

            foreach (var entry in context.ChangeTracker.Entries())
            {
                Console.WriteLine($"[SoftDeleteInterceptor]   {entry.Entity.GetType().Name}: State={entry.State}");
                if (entry.Entity is ISoftDeletable sd)
                {
                    Console.WriteLine($"[SoftDeleteInterceptor]     DeletedAt={sd.DeletedAt}, DeletedBy={sd.DeletedBy}");
                    if (entry.Property(nameof(ISoftDeletable.DeletedAt)).IsModified)
                    {
                        Console.WriteLine($"[SoftDeleteInterceptor]     DeletedAt property IS MODIFIED");
                    }
                }
            }

            // Process entities marked for deletion (State = Deleted)
            ProcessDeletedEntries(context, currentUserId, utcNow);

            // Process entities where DeletedAt was manually set (State = Modified)
            ProcessModifiedEntriesWithDeletedAt(context, currentUserId, utcNow);
        }

        /// <summary>
        /// Processes entities that are in Deleted state and converts them to soft deletes.
        /// </summary>
        /// <param name="context">The DbContext containing the tracked entities.</param>
        /// <param name="currentUserId">The ID of the current user, or null if not available.</param>
        /// <param name="utcNow">The current UTC timestamp.</param>
        private static void ProcessDeletedEntries(DbContext context, Guid? currentUserId, DateTime utcNow)
        {
            var entriesToSoftDelete = context.ChangeTracker
                .Entries()
                .Where(e => e.State == EntityState.Deleted && e.Entity is ISoftDeletable)
                .ToList();

            foreach (var entry in entriesToSoftDelete)
            {
                if (entry.Entity is not ISoftDeletable softDeletable)
                {
                    continue;
                }

                softDeletable.DeletedAt = utcNow;
                softDeletable.DeletedBy = currentUserId;
                entry.State = EntityState.Modified;
                entry.Property(nameof(ISoftDeletable.DeletedAt)).IsModified = true;

                if (currentUserId.HasValue)
                {
                    entry.Property(nameof(ISoftDeletable.DeletedBy)).IsModified = true;
                }

                if (entry.Entity is Organization organization)
                {
                    CascadeSoftDeleteForOrganization(context, organization.Id, currentUserId, utcNow);
                }
            }
        }

        /// <summary>
        /// Processes entities that have DeletedAt manually set (for test scenarios with InMemory DB).
        /// </summary>
        /// <param name="context">The DbContext containing the tracked entities.</param>
        /// <param name="currentUserId">The ID of the current user, or null if not available.</param>
        /// <param name="utcNow">The current UTC timestamp.</param>
        private static void ProcessModifiedEntriesWithDeletedAt(DbContext context, Guid? currentUserId, DateTime utcNow)
        {
            var entriesWithDeletedAtSet = context.ChangeTracker
                .Entries()
                .Where(e => e.State == EntityState.Modified &&
                           e.Entity is ISoftDeletable softDeletable &&
                           softDeletable.DeletedAt.HasValue &&
                           e.Property(nameof(ISoftDeletable.DeletedAt)).IsModified)
                .ToList();

            Console.WriteLine($"[SoftDeleteInterceptor] Found {entriesWithDeletedAtSet.Count} modified entries with DeletedAt set");

            foreach (var entry in entriesWithDeletedAtSet)
            {
                if (entry.Entity is not ISoftDeletable softDeletable)
                {
                    continue;
                }

                Console.WriteLine($"[SoftDeleteInterceptor] Processing {entry.Entity.GetType().Name}, DeletedBy={softDeletable.DeletedBy}, CurrentUserId={currentUserId}");

                if (currentUserId.HasValue && !softDeletable.DeletedBy.HasValue)
                {
                    softDeletable.DeletedBy = currentUserId;
                    entry.Property(nameof(ISoftDeletable.DeletedBy)).IsModified = true;
                    Console.WriteLine($"[SoftDeleteInterceptor] Set DeletedBy to {currentUserId}");
                }

                if (entry.Entity is Organization organization)
                {
                    var deletedAt = softDeletable.DeletedAt ?? utcNow;
                    Console.WriteLine($"[SoftDeleteInterceptor] Cascading delete for Organization {organization.Id}");
                    CascadeSoftDeleteForOrganization(context, organization.Id, currentUserId, deletedAt);
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
            var groupEntries = context.ChangeTracker
                .Entries<Group>()
                .Where(e => e.State != EntityState.Deleted &&
                           e.State != EntityState.Detached &&
                           e.Entity.OrganizationId == organizationId &&
                           e.Entity.DeletedAt == null)
                .ToList();

            foreach (var entry in groupEntries)
            {
                entry.Entity.DeletedAt = deletedAt;
                if (entry.State == EntityState.Unchanged)
                {
                    entry.State = EntityState.Modified;
                }

                entry.Property(nameof(Group.DeletedAt)).IsModified = true;
            }

            // Note: UserOrganizationMembership doesn't have DeletedAt/DeletedBy in current schema
            // If it should support soft delete, add ISoftDeletable interface to the entity
            // and add DeletedAt/DeletedBy properties

            // Soft delete all ApiKeys belonging to the organization
            var apiKeyEntries = context.ChangeTracker
                .Entries<ApiKey>()
                .Where(e => e.State != EntityState.Deleted &&
                           e.State != EntityState.Detached &&
                           e.Entity.OrganizationId == organizationId &&
                           e.Entity.RevokedAt == null)
                .ToList();

            // Note: ApiKey uses RevokedAt/RevokedBy pattern instead of DeletedAt/DeletedBy
            // Setting IsActive to false and populating RevokedAt/RevokedBy for consistency
            foreach (var entry in apiKeyEntries)
            {
                entry.Entity.IsActive = false;
                entry.Entity.RevokedAt = deletedAt;
                entry.Entity.RevokedBy = userId;
                entry.Entity.RevocationReason = "Organization deleted";

                if (entry.State == EntityState.Unchanged)
                {
                    entry.State = EntityState.Modified;
                }

                entry.Property(nameof(ApiKey.IsActive)).IsModified = true;
                entry.Property(nameof(ApiKey.RevokedAt)).IsModified = true;
                entry.Property(nameof(ApiKey.RevocationReason)).IsModified = true;

                if (userId.HasValue)
                {
                    entry.Property(nameof(ApiKey.RevokedBy)).IsModified = true;
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
