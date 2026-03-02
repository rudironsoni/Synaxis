// <copyright file="20260205000000_AddAuditLogPartitioning.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Migrations
{
    using Microsoft.EntityFrameworkCore.Migrations;

    /// <summary>
    /// Migration to convert AuditLogs table to PostgreSQL native declarative partitioning.
    /// Implements monthly range partitioning for efficient data retention management.
    /// </summary>
    public partial class AddAuditLogPartitioning : Migration
    {
        /// <summary>
        /// Applies the migration to convert AuditLogs table to partitioned table.
        /// Creates partition functions, migrates existing data, and sets up monthly partitioning.
        /// </summary>
        /// <param name="migrationBuilder">The migration builder instance.</param>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            DropExistingIndexes(migrationBuilder);
            RenameAuditLogsTable(migrationBuilder);
            CreatePartitionedAuditLogsTable(migrationBuilder);
            CreatePartitionedAuditLogsIndexes(migrationBuilder);
            CreatePartitionManagementFunction(migrationBuilder);
            CreatePartitionCleanupFunction(migrationBuilder);
            CreateCurrentAndNextMonthPartitions(migrationBuilder);
            MigrateAuditLogsData(migrationBuilder);
            CreateHistoricalPartitions(migrationBuilder);
            KeepBackupTableForVerification(migrationBuilder);
        }

        /// <summary>
        /// Reverts the migration by dropping partitioned table and restoring from backup.
        /// Removes partition management functions.
        /// </summary>
        /// <param name="migrationBuilder">The migration builder instance.</param>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RestoreAuditLogsBackup(migrationBuilder);
            DropPartitionFunctions(migrationBuilder);
        }

        private static void DropExistingIndexes(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS ""IX_AuditLogs_PartitionDate"";
                DROP INDEX IF EXISTS ""IX_AuditLogs_CreatedAt"";
            ");
        }

        private static void RenameAuditLogsTable(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE audit.""AuditLogs"" RENAME TO ""AuditLogs_backup"";
            ");
        }

        private static void CreatePartitionedAuditLogsTable(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE audit.""AuditLogs"" (
                    ""Id"" UUID NOT NULL,
                    ""OrganizationId"" UUID NULL,
                    ""UserId"" UUID NULL,
                    ""Action"" VARCHAR(200) NOT NULL,
                    ""EntityType"" VARCHAR(100) NULL,
                    ""EntityId"" VARCHAR(100) NULL,
                    ""PreviousValues"" JSONB NULL,
                    ""NewValues"" JSONB NULL,
                    ""IpAddress"" VARCHAR(45) NULL,
                    ""UserAgent"" VARCHAR(500) NULL,
                    ""CorrelationId"" VARCHAR(100) NULL,
                    ""CreatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""PartitionDate"" DATE NOT NULL,
                    CONSTRAINT ""PK_AuditLogs"" PRIMARY KEY (""Id"", ""PartitionDate""),
                    CONSTRAINT ""CK_AuditLog_Action"" CHECK (""Action"" IN ('Create', 'Update', 'Delete', 'Read', 'Login', 'Logout', 'ApiCall', 'PermissionChange', 'ConfigChange'))
                ) PARTITION BY RANGE (""PartitionDate"");
            ");
        }

        private static void CreatePartitionedAuditLogsIndexes(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE INDEX ""IX_AuditLogs_OrganizationId"" ON audit.""AuditLogs"" (""OrganizationId"");
                CREATE INDEX ""IX_AuditLogs_UserId"" ON audit.""AuditLogs"" (""UserId"");
                CREATE INDEX ""IX_AuditLogs_Action"" ON audit.""AuditLogs"" (""Action"");
                CREATE INDEX ""IX_AuditLogs_EntityType"" ON audit.""AuditLogs"" (""EntityType"");
                CREATE INDEX ""IX_AuditLogs_CreatedAt"" ON audit.""AuditLogs"" (""CreatedAt"");
                CREATE INDEX ""IX_AuditLogs_PartitionDate"" ON audit.""AuditLogs"" (""PartitionDate"");
                CREATE INDEX ""IX_AuditLogs_CorrelationId"" ON audit.""AuditLogs"" (""CorrelationId"");
            ");
        }

        private static void CreatePartitionManagementFunction(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION audit.ensure_auditlog_partition(target_date DATE)
                RETURNS TEXT
                LANGUAGE plpgsql
                AS $$
                DECLARE
                    partition_name TEXT;
                    start_date DATE;
                    end_date DATE;
                    create_sql TEXT;
                BEGIN
                    start_date := DATE_TRUNC('month', target_date)::DATE;
                    end_date := (start_date + INTERVAL '1 month')::DATE;
                    partition_name := format('""AuditLogs_%s""', TO_CHAR(start_date, 'YYYY_MM'));

                    IF EXISTS (
                        SELECT 1 FROM pg_tables
                        WHERE schemaname = 'audit'
                        AND tablename = partition_name
                    ) THEN
                        RETURN format('Partition %s already exists', partition_name);
                    END IF;

                    create_sql := format(
                        'CREATE TABLE audit.%I PARTITION OF audit.""AuditLogs"" FOR VALUES FROM (%L) TO (%L)',
                        partition_name, start_date, end_date
                    );
                    EXECUTE create_sql;

                    RETURN format('Created partition %s for range [%s, %s)', partition_name, start_date, end_date);
                END;
                $$;
            ");
        }

        private static void CreatePartitionCleanupFunction(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION audit.cleanup_auditlog_partitions(retention_days INTEGER)
                RETURNS TABLE(dropped_partition TEXT, drop_reason TEXT)
                LANGUAGE plpgsql
                AS $$
                DECLARE
                    partition_record RECORD;
                    partition_start DATE;
                    cutoff_date DATE;
                BEGIN
                    cutoff_date := (CURRENT_DATE - retention_days)::DATE;

                    FOR partition_record IN
                        SELECT child.relname AS partition_name,
                               pg_get_expr(child.relminexpr, child.oid) AS partition_bound
                        FROM pg_inherits
                        JOIN pg_class parent ON pg_inherits.inhparent = parent.oid
                        JOIN pg_class child ON pg_inherits.inhrelid = child.oid
                        WHERE parent.relname = 'AuditLogs'
                        AND parent.relnamespace = (SELECT oid FROM pg_namespace WHERE nspname = 'audit')
                    LOOP
                        IF partition_record.partition_bound ~ 'FROM \\(\''([^'']+)\''\\)' THEN
                            partition_start := (regexp_match(partition_record.partition_bound, 'FROM \\(\''([^'']+)\''\\)'))[1]::DATE;

                            IF partition_start < DATE_TRUNC('month', cutoff_date)::DATE THEN
                                EXECUTE format('DROP TABLE IF EXISTS audit.%I', partition_record.partition_name);
                                dropped_partition := partition_record.partition_name;
                                drop_reason := format('Partition start date %s is older than retention cutoff %s', partition_start, cutoff_date);
                                RETURN NEXT;
                            END IF;
                        END IF;
                    END LOOP;

                    RETURN;
                END;
                $$;
            ");
        }

        private static void CreateCurrentAndNextMonthPartitions(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                SELECT audit.ensure_auditlog_partition(CURRENT_DATE);
                SELECT audit.ensure_auditlog_partition(CURRENT_DATE + INTERVAL '1 month');
            ");
        }

        private static void MigrateAuditLogsData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO audit.""AuditLogs"" (
                    ""Id"", ""OrganizationId"", ""UserId"", ""Action"", ""EntityType"", ""EntityId"",
                    ""PreviousValues"", ""NewValues"", ""IpAddress"", ""UserAgent"", ""CorrelationId"",
                    ""CreatedAt"", ""PartitionDate""
                )
                SELECT
                    ""Id"", ""OrganizationId"", ""UserId"", ""Action"", ""EntityType"", ""EntityId"",
                    ""PreviousValues""::JSONB, ""NewValues""::JSONB, ""IpAddress"", ""UserAgent"", ""CorrelationId"",
                    ""CreatedAt"", COALESCE(""PartitionDate""::DATE, ""CreatedAt""::DATE)
                FROM audit.""AuditLogs_backup"";
            ");
        }

        private static void CreateHistoricalPartitions(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    min_date DATE;
                    current_month DATE;
                BEGIN
                    SELECT MIN(""PartitionDate""::DATE) INTO min_date FROM audit.""AuditLogs_backup"";
                    current_month := DATE_TRUNC('month', min_date)::DATE;

                    WHILE current_month < DATE_TRUNC('month', CURRENT_DATE)::DATE LOOP
                        PERFORM audit.ensure_auditlog_partition(current_month);
                        current_month := (current_month + INTERVAL '1 month')::DATE;
                    END LOOP;
                END;
                $$;
            ");
        }

        private static void KeepBackupTableForVerification(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- DROP TABLE IF EXISTS audit.""AuditLogs_backup"";
                -- Keeping backup table for safety. Manual verification recommended before dropping.
            ");
        }

        private static void RestoreAuditLogsBackup(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS audit.""AuditLogs"" CASCADE;
                ALTER TABLE IF EXISTS audit.""AuditLogs_backup"" RENAME TO ""AuditLogs"";
            ");
        }

        private static void DropPartitionFunctions(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP FUNCTION IF EXISTS audit.ensure_auditlog_partition(DATE);
                DROP FUNCTION IF EXISTS audit.cleanup_auditlog_partitions(INTEGER);
            ");
        }
    }
}
