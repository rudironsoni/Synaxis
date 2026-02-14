// <copyright file="ConversationService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Synaxis.Core.Models;
    using Synaxis.Infrastructure.Data;

    /// <summary>
    /// Service for managing conversation state and turn-based conversation flow.
    /// </summary>
    public class ConversationService
    {
        private readonly SynaxisDbContext _dbContext;
        private readonly ILogger<ConversationService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConversationService"/> class.
        /// </summary>
        /// <param name="dbContext">The database context.</param>
        /// <param name="logger">The logger instance.</param>
        public ConversationService(
            SynaxisDbContext dbContext,
            ILogger<ConversationService> logger)
        {
            this._dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a new conversation.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="title">The conversation title.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created conversation.</returns>
        public async Task<Conversation> CreateConversationAsync(
            Guid userId,
            Guid organizationId,
            string title = null,
            CancellationToken cancellationToken = default)
        {
            var conversation = new Conversation
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                OrganizationId = organizationId,
                Title = title ?? "New Conversation",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };

            this._dbContext.Conversations.Add(conversation);
            await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            this._logger.LogInformation(
                "Created conversation {ConversationId} for user {UserId} in organization {OrganizationId}",
                conversation.Id,
                userId,
                organizationId);

            return conversation;
        }

        /// <summary>
        /// Gets a conversation by ID.
        /// </summary>
        /// <param name="conversationId">The conversation ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The conversation, or null if not found.</returns>
        public async Task<Conversation> GetConversationAsync(
            Guid conversationId,
            CancellationToken cancellationToken = default)
        {
            return await this._dbContext.Conversations
                .Include(c => c.Turns.OrderBy(t => t.TurnNumber))
                .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Gets conversations for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="skip">The number of records to skip.</param>
        /// <param name="take">The number of records to take.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of conversations.</returns>
        public async Task<List<Conversation>> GetUserConversationsAsync(
            Guid userId,
            Guid organizationId,
            int skip = 0,
            int take = 50,
            CancellationToken cancellationToken = default)
        {
            return await this._dbContext.Conversations
                .Where(c => c.UserId == userId && c.OrganizationId == organizationId)
                .OrderByDescending(c => c.UpdatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Adds a turn to a conversation.
        /// </summary>
        /// <param name="conversationId">The conversation ID.</param>
        /// <param name="role">The role (user, assistant, system).</param>
        /// <param name="content">The message content.</param>
        /// <param name="metadata">Optional metadata for the turn.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created conversation turn.</returns>
        public async Task<ConversationTurn> AddTurnAsync(
            Guid conversationId,
            string role,
            string content,
            Dictionary<string, string> metadata = null,
            CancellationToken cancellationToken = default)
        {
            var conversation = await this._dbContext.Conversations
                .Include(c => c.Turns)
                .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken)
                .ConfigureAwait(false);

            if (conversation == null)
            {
                throw new InvalidOperationException($"Conversation {conversationId} not found");
            }

            var turnNumber = conversation.Turns.Count + 1;
            var turn = new ConversationTurn
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                TurnNumber = turnNumber,
                Role = role,
                Content = content,
                Metadata = metadata,
                CreatedAt = DateTime.UtcNow
            };

            this._dbContext.ConversationTurns.Add(turn);

            // Update conversation timestamp
            conversation.UpdatedAt = DateTime.UtcNow;

            await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            this._logger.LogInformation(
                "Added turn {TurnNumber} to conversation {ConversationId} with role {Role}",
                turnNumber,
                conversationId,
                role);

            return turn;
        }

        /// <summary>
        /// Gets the conversation history for a conversation.
        /// </summary>
        /// <param name="conversationId">The conversation ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of conversation turns.</returns>
        public async Task<List<ConversationTurn>> GetConversationHistoryAsync(
            Guid conversationId,
            CancellationToken cancellationToken = default)
        {
            return await this._dbContext.ConversationTurns
                .Where(t => t.ConversationId == conversationId)
                .OrderBy(t => t.TurnNumber)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Updates a conversation title.
        /// </summary>
        /// <param name="conversationId">The conversation ID.</param>
        /// <param name="title">The new title.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The updated conversation.</returns>
        public async Task<Conversation> UpdateConversationTitleAsync(
            Guid conversationId,
            string title,
            CancellationToken cancellationToken = default)
        {
            var conversation = await this._dbContext.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken)
                .ConfigureAwait(false);

            if (conversation == null)
            {
                throw new InvalidOperationException($"Conversation {conversationId} not found");
            }

            conversation.Title = title;
            conversation.UpdatedAt = DateTime.UtcNow;

            await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            this._logger.LogInformation(
                "Updated title for conversation {ConversationId} to '{Title}'",
                conversationId,
                title);

            return conversation;
        }

        /// <summary>
        /// Archives a conversation (marks as inactive).
        /// </summary>
        /// <param name="conversationId">The conversation ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ArchiveConversationAsync(
            Guid conversationId,
            CancellationToken cancellationToken = default)
        {
            var conversation = await this._dbContext.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken)
                .ConfigureAwait(false);

            if (conversation == null)
            {
                throw new InvalidOperationException($"Conversation {conversationId} not found");
            }

            conversation.IsActive = false;
            conversation.UpdatedAt = DateTime.UtcNow;

            await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            this._logger.LogInformation("Archived conversation {ConversationId}", conversationId);
        }

        /// <summary>
        /// Deletes a conversation and all its turns.
        /// </summary>
        /// <param name="conversationId">The conversation ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task DeleteConversationAsync(
            Guid conversationId,
            CancellationToken cancellationToken = default)
        {
            var conversation = await this._dbContext.Conversations
                .Include(c => c.Turns)
                .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken)
                .ConfigureAwait(false);

            if (conversation == null)
            {
                throw new InvalidOperationException($"Conversation {conversationId} not found");
            }

            this._dbContext.ConversationTurns.RemoveRange(conversation.Turns);
            this._dbContext.Conversations.Remove(conversation);

            await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            this._logger.LogInformation("Deleted conversation {ConversationId}", conversationId);
        }

        /// <summary>
        /// Gets the turn count for a conversation.
        /// </summary>
        /// <param name="conversationId">The conversation ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The number of turns in the conversation.</returns>
        public async Task<int> GetTurnCountAsync(
            Guid conversationId,
            CancellationToken cancellationToken = default)
        {
            return await this._dbContext.ConversationTurns
                .CountAsync(t => t.ConversationId == conversationId, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
