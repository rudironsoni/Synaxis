// <copyright file="RollbackScriptTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Tests.Migrations
{
    using System;
    using System.IO;
    using FluentAssertions;
    using Xunit;

    /// <summary>
    /// Tests for migration rollback scripts.
    /// </summary>
    public class RollbackScriptTests
    {
        private readonly string _repoRoot;

        /// <summary>
        /// Initializes a new instance of the <see cref="RollbackScriptTests"/> class.
        /// </summary>
        public RollbackScriptTests()
        {
            // Navigate from test project to repo root
            _repoRoot = Path.GetFullPath(
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "..",
                    "..",
                    "..",
                    "..",
                    ".."));
        }

        /// <summary>
        /// Verifies that the PowerShell rollback script exists.
        /// </summary>
        [Fact]
        public void PowerShellScript_Exists()
        {
            // Arrange
            var scriptPath = Path.Combine(_repoRoot, "scripts", "rollback-migration.ps1");

            // Act & Assert
            File.Exists(scriptPath).Should().BeTrue($"PowerShell script should exist at {scriptPath}");
        }

        /// <summary>
        /// Verifies that the Bash rollback script exists.
        /// </summary>
        [Fact]
        public void BashScript_Exists()
        {
            // Arrange
            var scriptPath = Path.Combine(_repoRoot, "scripts", "rollback-migration.sh");

            // Act & Assert
            File.Exists(scriptPath).Should().BeTrue($"Bash script should exist at {scriptPath}");
        }

        /// <summary>
        /// Verifies that the PowerShell script accepts migration name parameter.
        /// </summary>
        [Fact]
        public void PowerShellScript_AcceptsMigrationParameter()
        {
            // Arrange
            var scriptPath = Path.Combine(_repoRoot, "scripts", "rollback-migration.ps1");

            // Act
            var content = File.ReadAllText(scriptPath);

            // Assert
            content.Should().Contain("[string]$Migration");
            content.Should().Contain("Mandatory = $true");
        }

        /// <summary>
        /// Verifies that the Bash script accepts migration name parameter.
        /// </summary>
        [Fact]
        public void BashScript_AcceptsMigrationParameter()
        {
            // Arrange
            var scriptPath = Path.Combine(_repoRoot, "scripts", "rollback-migration.sh");

            // Act
            var content = File.ReadAllText(scriptPath);

            // Assert
            content.Should().Contain("MIGRATION");
            content.Should().Contain("Usage:");
        }

        /// <summary>
        /// Verifies that scripts create backups before rollback.
        /// </summary>
        [Fact]
        public void Scripts_CreateBackupsBeforeRollback()
        {
            // Arrange
            var psScriptPath = Path.Combine(_repoRoot, "scripts", "rollback-migration.ps1");
            var bashScriptPath = Path.Combine(_repoRoot, "scripts", "rollback-migration.sh");

            // Act
            var psContent = File.ReadAllText(psScriptPath);
            var bashContent = File.ReadAllText(bashScriptPath);

            // Assert
            psContent.Should().Contain("Backup");
            psContent.Should().Contain("pg_dump");
            bashContent.Should().Contain("backup");
            bashContent.Should().Contain("pg_dump");
        }

        /// <summary>
        /// Verifies that the Bash script is executable.
        /// </summary>
        [Fact]
        public void BashScript_IsExecutable()
        {
            // Arrange
            var scriptPath = Path.Combine(_repoRoot, "scripts", "rollback-migration.sh");

            // Skip on Windows
            if (OperatingSystem.IsWindows())
            {
                return;
            }

            // Act
            var fileInfo = new FileInfo(scriptPath);

            // Assert
            // Check if owner has execute permission (Unix permissions)
            var mode = fileInfo.UnixFileMode;
            mode.HasFlag(UnixFileMode.UserExecute).Should().BeTrue("Bash script should be executable");
        }

        /// <summary>
        /// Verifies that scripts support connection string parameter.
        /// </summary>
        [Fact]
        public void Scripts_SupportConnectionStringParameter()
        {
            // Arrange
            var psScriptPath = Path.Combine(_repoRoot, "scripts", "rollback-migration.ps1");
            var bashScriptPath = Path.Combine(_repoRoot, "scripts", "rollback-migration.sh");

            // Act
            var psContent = File.ReadAllText(psScriptPath);
            var bashContent = File.ReadAllText(bashScriptPath);

            // Assert
            psContent.Should().Contain("ConnectionString");
            bashContent.Should().Contain("CONNECTION_STRING");
        }

        /// <summary>
        /// Verifies that scripts use EF Core migrations.
        /// </summary>
        [Fact]
        public void Scripts_UseEfCoreMigrations()
        {
            // Arrange
            var psScriptPath = Path.Combine(_repoRoot, "scripts", "rollback-migration.ps1");
            var bashScriptPath = Path.Combine(_repoRoot, "scripts", "rollback-migration.sh");

            // Act
            var psContent = File.ReadAllText(psScriptPath);
            var bashContent = File.ReadAllText(bashScriptPath);

            // Assert
            psContent.Should().Contain("dotnet ef database update");
            bashContent.Should().Contain("dotnet ef database update");
        }

        /// <summary>
        /// Verifies that scripts include error handling.
        /// </summary>
        [Fact]
        public void Scripts_IncludeErrorHandling()
        {
            // Arrange
            var psScriptPath = Path.Combine(_repoRoot, "scripts", "rollback-migration.ps1");
            var bashScriptPath = Path.Combine(_repoRoot, "scripts", "rollback-migration.sh");

            // Act
            var psContent = File.ReadAllText(psScriptPath);
            var bashContent = File.ReadAllText(bashScriptPath);

            // Assert
            psContent.Should().Contain("try");
            psContent.Should().Contain("catch");
            bashContent.Should().Contain("set -e");
        }
    }
}
