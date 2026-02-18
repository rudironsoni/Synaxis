// <copyright file="CommandBuilder.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.TestUtilities.Builders;

/// <summary>
/// Builder for creating commands in tests.
/// </summary>
/// <typeparam name="TCommand">The type of command to build.</typeparam>
public class CommandBuilder<TCommand>
    where TCommand : class, new()
{
    private readonly TCommand _command;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandBuilder{TCommand}"/> class.
    /// </summary>
    public CommandBuilder()
    {
        _command = new TCommand();
    }

    /// <summary>
    /// Sets a property on the command using reflection.
    /// </summary>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="value">The value to set.</param>
    /// <returns>The builder for method chaining.</returns>
    public CommandBuilder<TCommand> WithProperty(string propertyName, object? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(propertyName);

        var property = typeof(TCommand).GetProperty(propertyName);
        if (property != null && property.CanWrite)
        {
            property.SetValue(_command, value);
        }

        return this;
    }

    /// <summary>
    /// Sets the command identifier.
    /// </summary>
    /// <param name="commandId">The command identifier.</param>
    /// <returns>The builder for method chaining.</returns>
    public CommandBuilder<TCommand> WithCommandId(Guid commandId)
    {
        var property = typeof(TCommand).GetProperty("Id") ??
                       typeof(TCommand).GetProperty("CommandId") ??
                       typeof(TCommand).GetProperty("CommandId", System.Reflection.BindingFlags.IgnoreCase);

        if (property != null && property.CanWrite)
        {
            if (property.PropertyType == typeof(Guid))
            {
                property.SetValue(_command, commandId);
            }
            else if (property.PropertyType == typeof(string))
            {
                property.SetValue(_command, commandId.ToString());
            }
        }

        return this;
    }

    /// <summary>
    /// Sets the timestamp for the command.
    /// </summary>
    /// <param name="timestamp">The timestamp.</param>
    /// <returns>The builder for method chaining.</returns>
    public CommandBuilder<TCommand> WithTimestamp(DateTime timestamp)
    {
        var property = typeof(TCommand).GetProperty("Timestamp") ??
                       typeof(TCommand).GetProperty("CreatedAt") ??
                       typeof(TCommand).GetProperty("OccurredOn");

        if (property != null && property.CanWrite)
        {
            property.SetValue(_command, timestamp);
        }

        return this;
    }

    /// <summary>
    /// Sets the timestamp to the current UTC time.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    public CommandBuilder<TCommand> WithCurrentTimestamp()
    {
        return WithTimestamp(DateTime.UtcNow);
    }

    /// <summary>
    /// Sets the user identifier for the command.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>The builder for method chaining.</returns>
    public CommandBuilder<TCommand> WithUserId(string userId)
    {
        ArgumentException.ThrowIfNullOrEmpty(userId);

        var property = typeof(TCommand).GetProperty("UserId") ??
                       typeof(TCommand).GetProperty("InitiatedBy");

        if (property != null && property.CanWrite)
        {
            property.SetValue(_command, userId);
        }

        return this;
    }

    /// <summary>
    /// Sets the correlation identifier for the command.
    /// </summary>
    /// <param name="correlationId">The correlation identifier.</param>
    /// <returns>The builder for method chaining.</returns>
    public CommandBuilder<TCommand> WithCorrelationId(string correlationId)
    {
        ArgumentException.ThrowIfNullOrEmpty(correlationId);

        var property = typeof(TCommand).GetProperty("CorrelationId");

        if (property != null && property.CanWrite)
        {
            property.SetValue(_command, correlationId);
        }

        return this;
    }

    /// <summary>
    /// Sets the aggregate identifier for the command.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <returns>The builder for method chaining.</returns>
    public CommandBuilder<TCommand> WithAggregateId(string aggregateId)
    {
        ArgumentException.ThrowIfNullOrEmpty(aggregateId);

        var property = typeof(TCommand).GetProperty("AggregateId");

        if (property != null && property.CanWrite)
        {
            property.SetValue(_command, aggregateId);
        }

        return this;
    }

    /// <summary>
    /// Builds the command.
    /// </summary>
    /// <returns>The constructed command.</returns>
    public TCommand Build() => _command;

    /// <summary>
    /// Creates multiple commands with the same configuration.
    /// </summary>
    /// <param name="count">The number of commands to create.</param>
    /// <returns>A list of commands.</returns>
    public List<TCommand> BuildMany(int count)
    {
        var commands = new List<TCommand>();
        for (var i = 0; i < count; i++)
        {
            commands.Add(Build());
        }

        return commands;
    }

    /// <summary>
    /// Implicitly converts the builder to the command type.
    /// </summary>
    /// <param name="builder">The builder to convert.</param>
    /// <returns>The constructed command.</returns>
    public static implicit operator TCommand(CommandBuilder<TCommand> builder) => builder.Build();
}

/// <summary>
/// Non-generic command builder for creating commands.
/// </summary>
public static class CommandBuilder
{
    /// <summary>
    /// Creates a new command builder for the specified command type.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to build.</typeparam>
    /// <returns>A new command builder.</returns>
    public static CommandBuilder<TCommand> Create<TCommand>()
        where TCommand : class, new()
    {
        return new CommandBuilder<TCommand>();
    }

    /// <summary>
    /// Creates a new command with default values.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to create.</typeparam>
    /// <returns>A new command instance.</returns>
    public static TCommand CreateDefault<TCommand>()
        where TCommand : class, new()
    {
        return new TCommand();
    }

    /// <summary>
    /// Creates multiple commands with default values.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to create.</typeparam>
    /// <param name="count">The number of commands to create.</param>
    /// <returns>A list of commands.</returns>
    public static List<TCommand> CreateMany<TCommand>(int count)
        where TCommand : class, new()
    {
        var commands = new List<TCommand>();
        for (var i = 0; i < count; i++)
        {
            commands.Add(new TCommand());
        }

        return commands;
    }
}
