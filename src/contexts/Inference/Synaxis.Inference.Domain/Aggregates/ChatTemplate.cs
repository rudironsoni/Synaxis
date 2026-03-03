// <copyright file="ChatTemplate.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Aggregates;

using Synaxis.Inference.Domain.Events;
using Synaxis.Infrastructure.EventSourcing;

/// <summary>
/// Aggregate root representing a chat template for structured prompts.
/// </summary>
public class ChatTemplate : AggregateRoot
{
    /// <summary>
    /// Gets the template identifier.
    /// </summary>
    public new Guid Id { get; private set; }

    /// <summary>
    /// Gets the template name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the template description.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets the template content.
    /// </summary>
    public string TemplateContent { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the template parameters.
    /// </summary>
    public IList<TemplateParameter> Parameters { get; private set; } = new List<TemplateParameter>();

    /// <summary>
    /// Gets the template category.
    /// </summary>
    public string Category { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this is a system template.
    /// </summary>
    public bool IsSystemTemplate { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the template is active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Gets the usage count.
    /// </summary>
    public int UsageCount { get; private set; }

    /// <summary>
    /// Gets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the last updated timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Creates a new chat template.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="name">The name of the template.</param>
    /// <param name="description">The description of the template.</param>
    /// <param name="templateContent">The content of the template.</param>
    /// <param name="parameters">The parameters of the template.</param>
    /// <param name="category">The category of the template.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="isSystemTemplate">Whether this is a system template.</param>
    /// <returns>A new chat template instance.</returns>
    public static ChatTemplate Create(
        Guid id,
        string name,
        string? description,
        string templateContent,
        IList<TemplateParameter> parameters,
        string category,
        Guid tenantId,
        bool isSystemTemplate = false)
    {
        var template = new ChatTemplate();
        var @event = new ChatTemplateCreated
        {
            TemplateId = id,
            Name = name,
            Description = description,
            TemplateContent = templateContent,
            Parameters = parameters,
            Category = category,
            TenantId = tenantId,
            IsSystemTemplate = isSystemTemplate,
            Timestamp = DateTime.UtcNow,
        };

        template.ApplyEvent(@event);
        return template;
    }

    /// <summary>
    /// Updates the template.
    /// </summary>
    /// <param name="name">The name of the template.</param>
    /// <param name="description">The description of the template.</param>
    /// <param name="templateContent">The content of the template.</param>
    /// <param name="parameters">The parameters of the template.</param>
    /// <param name="category">The category of the template.</param>
    public void Update(string name, string? description, string templateContent, IList<TemplateParameter> parameters, string category)
    {
        if (this.IsSystemTemplate)
        {
            throw new InvalidOperationException("Cannot update a system template.");
        }

        var @event = new ChatTemplateUpdated
        {
            TemplateId = this.Id,
            Name = name,
            Description = description,
            TemplateContent = templateContent,
            Parameters = parameters,
            Category = category,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Renders the template with provided parameter values.
    /// </summary>
    /// <param name="parameterValues">The parameter values to use when rendering.</param>
    /// <returns>The rendered template content.</returns>
    public string Render(IReadOnlyDictionary<string, string> parameterValues)
    {
        // Validate all required parameters are provided
        var missingParam = this.Parameters.FirstOrDefault(p => p.IsRequired && !parameterValues.ContainsKey(p.Name));
        if (missingParam != null)
        {
            throw new ArgumentException($"Required parameter '{missingParam.Name}' is missing.", nameof(parameterValues));
        }

        var result = this.TemplateContent;
        foreach (var param in this.Parameters)
        {
            var value = parameterValues.GetValueOrDefault(param.Name, param.DefaultValue ?? string.Empty);
            result = result.Replace($"{{{param.Name}}}", value);
        }

        // Record usage
        var usageEvent = new ChatTemplateUsed
        {
            TemplateId = this.Id,
            Timestamp = DateTime.UtcNow,
        };
        this.ApplyEvent(usageEvent);

        return result;
    }

    /// <summary>
    /// Activates the template.
    /// </summary>
    public void Activate()
    {
        if (this.IsActive)
        {
            return;
        }

        var @event = new ChatTemplateActivated
        {
            TemplateId = this.Id,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Deactivates the template.
    /// </summary>
    public void Deactivate()
    {
        if (!this.IsActive)
        {
            return;
        }

        var @event = new ChatTemplateDeactivated
        {
            TemplateId = this.Id,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Deletes the template.
    /// </summary>
    public void Delete()
    {
        if (this.IsSystemTemplate)
        {
            throw new InvalidOperationException("Cannot delete a system template.");
        }

        var @event = new ChatTemplateDeleted
        {
            TemplateId = this.Id,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <inheritdoc/>
    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case ChatTemplateCreated created:
                this.ApplyCreated(created);
                break;
            case ChatTemplateUpdated updated:
                this.ApplyUpdated(updated);
                break;
            case ChatTemplateUsed:
                this.ApplyUsed();
                break;
            case ChatTemplateActivated:
                this.ApplyActivated();
                break;
            case ChatTemplateDeactivated:
                this.ApplyDeactivated();
                break;
            case ChatTemplateDeleted:
                this.ApplyDeleted();
                break;
        }
    }

    private void ApplyCreated(ChatTemplateCreated @event)
    {
        this.Id = @event.TemplateId;
        this.Name = @event.Name;
        this.Description = @event.Description;
        this.TemplateContent = @event.TemplateContent;
        this.Parameters = @event.Parameters;
        this.Category = @event.Category;
        this.TenantId = @event.TenantId;
        this.IsSystemTemplate = @event.IsSystemTemplate;
        this.IsActive = true;
        this.UsageCount = 0;
        this.CreatedAt = @event.Timestamp;
        this.UpdatedAt = @event.Timestamp;
    }

    private void ApplyUpdated(ChatTemplateUpdated @event)
    {
        this.Name = @event.Name;
        this.Description = @event.Description;
        this.TemplateContent = @event.TemplateContent;
        this.Parameters = @event.Parameters;
        this.Category = @event.Category;
        this.UpdatedAt = @event.Timestamp;
    }

    private void ApplyUsed()
    {
        this.UsageCount++;
    }

    private void ApplyActivated()
    {
        this.IsActive = true;
        this.UpdatedAt = DateTime.UtcNow;
    }

    private void ApplyDeactivated()
    {
        this.SetInactive();
    }

    private void ApplyDeleted()
    {
        this.SetInactive();
    }

    private void SetInactive()
    {
        this.IsActive = false;
        this.UpdatedAt = DateTime.UtcNow;
    }
}
