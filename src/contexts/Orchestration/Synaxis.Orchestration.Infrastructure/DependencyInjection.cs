// <copyright file="DependencyInjection.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Infrastructure;

using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Synaxis.Orchestration.Infrastructure.Workers;

/// <summary>
/// Extension methods for registering Orchestration infrastructure services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Orchestration background workers to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOrchestrationWorkers(this IServiceCollection services)
    {
        services.AddQuartz(q =>
        {
            // Workflow Timeout Monitor - runs every 5 minutes
            q.AddJob<WorkflowTimeoutMonitor>(opts => opts.WithIdentity("workflow-timeout-monitor"));
            q.AddTrigger(opts => opts
                .ForJob("workflow-timeout-monitor")
                .WithIdentity("workflow-timeout-trigger")
                .WithSimpleSchedule(x => x
                    .WithIntervalInMinutes(5)
                    .RepeatForever()));

            // Saga Compensation Worker - runs every 30 seconds
            q.AddJob<SagaCompensationWorker>(opts => opts.WithIdentity("saga-compensation-worker"));
            q.AddTrigger(opts => opts
                .ForJob("saga-compensation-worker")
                .WithIdentity("saga-compensation-trigger")
                .WithSimpleSchedule(x => x
                    .WithIntervalInSeconds(30)
                    .RepeatForever()));

            // Activity Retry Worker - runs every minute
            q.AddJob<ActivityRetryWorker>(opts => opts.WithIdentity("activity-retry-worker"));
            q.AddTrigger(opts => opts
                .ForJob("activity-retry-worker")
                .WithIdentity("activity-retry-trigger")
                .WithSimpleSchedule(x => x
                    .WithIntervalInMinutes(1)
                    .RepeatForever()));

            // Workflow Cleanup Worker - runs daily at 2 AM
            q.AddJob<WorkflowCleanupWorker>(opts => opts.WithIdentity("workflow-cleanup-worker"));
            q.AddTrigger(opts => opts
                .ForJob("workflow-cleanup-worker")
                .WithIdentity("workflow-cleanup-trigger")
                .WithCronSchedule("0 0 2 * * ?")); // 2:00 AM daily
        });

        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

        return services;
    }
}
