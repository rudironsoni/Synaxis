// <copyright file="GhConfigWriter.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Identity.Strategies.GitHub
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Utility class for writing GitHub CLI configuration files.
    /// </summary>
    public static class GhConfigWriter
    {
        /// <summary>
        /// Writes a GitHub token to the gh CLI hosts configuration file.
        /// </summary>
        /// <param name="token">The OAuth token to write.</param>
        /// <param name="user">The username to associate with the token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task WriteTokenAsync(string token, string user = "synaxis-user")
        {
            var path = PrepareConfigDirectory();
            var existing = await ReadExistingConfigAsync(path).ConfigureAwait(false);
            var hostBlock = BuildGitHubHostBlock(user, token);

            if (string.IsNullOrWhiteSpace(existing))
            {
                await File.WriteAllTextAsync(path, hostBlock).ConfigureAwait(false);
                return;
            }

            var updated = ReplaceOrAppendGitHubBlock(existing, hostBlock);
            await File.WriteAllTextAsync(path, updated).ConfigureAwait(false);
        }

        private static string PrepareConfigDirectory()
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var cfgDir = Path.Combine(home, ".config", "gh");
            var path = Path.Combine(cfgDir, "hosts.yml");

            if (!Directory.Exists(cfgDir))
            {
                Directory.CreateDirectory(cfgDir);
            }

            return path;
        }

        private static async Task<string> ReadExistingConfigAsync(string path)
        {
            if (File.Exists(path))
            {
                return await File.ReadAllTextAsync(path).ConfigureAwait(false);
            }

            return string.Empty;
        }

        private static string BuildGitHubHostBlock(string user, string token)
        {
            var hostBlock = new StringBuilder();
            hostBlock.AppendLine("github.com:");
            hostBlock.AppendLine($"  user: {user}");
            hostBlock.AppendLine($"  oauth_token: {token}");
            return hostBlock.ToString();
        }

        private static string ReplaceOrAppendGitHubBlock(string existing, string hostBlock)
        {
            var lines = existing.Replace("\r\n", "\n").Split('\n');
            var outSb = new StringBuilder();
            bool inGithubBlock = false;
            bool replaced = false;

            foreach (var line in lines)
            {
                var trimmed = line.TrimStart();
                if (!inGithubBlock && trimmed.StartsWith("github.com:", StringComparison.Ordinal))
                {
                    outSb.Append(hostBlock);
                    inGithubBlock = true;
                    replaced = true;
                    continue;
                }

                if (inGithubBlock)
                {
                    if (!line.StartsWith(' ') && !string.IsNullOrWhiteSpace(line))
                    {
                        inGithubBlock = false;
                    }
                    else
                    {
                        continue;
                    }
                }

                outSb.AppendLine(line);
            }

            if (!replaced)
            {
                if (outSb.Length > 0 && !outSb.ToString().EndsWith('\n'))
                {
                    outSb.AppendLine();
                }

                outSb.Append(hostBlock);
            }

            return outSb.ToString();
        }
    }
}
