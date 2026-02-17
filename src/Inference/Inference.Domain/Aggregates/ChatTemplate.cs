// <copyright file="ChatTemplate.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Aggregates;

using Synaxis.Infrastructure.EventSourcing;
using Synaxis.Inference.Domain.Events;

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
    public List<TemplateParameter> Parameters { get; private set; } = new();

    /// <summary>
    /// Gets the template category.
    /// </summary>
    public string Category { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Gets whether this is a system template.
    /// </summary>
    public bool IsSystemTemplate { get; private set; }

    /// <summary>
    /// Gets whether the template is active.
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
    public static ChatTemplate Create(
        Guid id,
        string name,
        string? description,
        string templateContent,
        List<TemplateParameter> parameters,
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
    public void Update(string name, string? description, string templateContent, List<TemplateParameter> parameters, string category)
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
    public string Render(Dictionary<string, string> parameterValues)
    {
        // Validate all required parameters are provided
        foreach (var param in this.Parameters.Where(p => p.IsRequired))
        {
            if (!parameterValues.ContainsKey(param.Name))
            {
                throw new ArgumentException($"Required parameter '{param.Name}' is missing.");
            }
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
        this.IsActive = false;
        this.UpdatedAt = DateTime.UtcNow;
    }

    private void ApplyDeleted()
    {
        this.IsActive = false;
        this.UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Represents a template parameter.
/// </summary>
public class TemplateParameter
{
    /// <summary>
    /// Gets or sets the parameter name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parameter description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the parameter type.
    /// </summary>
    public ParameterType Type { get; set; } = ParameterType.String;

    /// <summary>
    /// Gets or sets whether the parameter is required.
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// Gets or sets the default value.
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets the validation regex pattern.
    /// </summary>
    public string? ValidationPattern { get; set; }

    /// <summary>
    /// Gets or sets the allowed values for enum type.
    /// </summary>
    public List<string> AllowedValues { get; set; } = new();
}

/// <summary>
/// Represents parameter types.
/// </summary>
public enum ParameterType
{
    /// <summary>
    /// String parameter.
    /// </summary>
    String,

    /// <summary>
    /// Integer parameter.
    /// </summary>
    Integer,

    /// <summary>
    /// Decimal parameter.
    /// </summary>
    Decimal,

    /// <summary>
    /// Boolean parameter.
    /// </summary>
    Boolean,

    /// <summary>
    /// Enum parameter with allowed values.
    /// </summary>
    Enum,

    /// <summary>
    /// DateTime parameter.
    /// </summary>
    DateTime,

    /// <summary>
    /// JSON parameter.
    /// </summary>
    Json,
}
