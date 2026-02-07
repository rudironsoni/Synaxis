// <copyright file="GhConfigWriter.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Identity.Strategies.GitHub
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    public static class GhConfigWriter
    {
        public static async Task WriteTokenAsync(string token, string user = "synaxis-user")
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var cfgDir = Path.Combine(home, ".config", "gh");
            var path = Path.Combine(cfgDir, "hosts.yml");

            if (!Directory.Exists(cfgDir))
            {
                Directory.CreateDirectory(cfgDir);
            }

            string existing = string.Empty;
            if (File.Exists(path))
            {
                existing = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            }

            // Very small YAML manipulation: find github.com block, replace or append
            var hostBlock = new StringBuilder();
            hostBlock.AppendLine("github.com:");
            hostBlock.AppendLine($"  user: {user}");
            hostBlock.AppendLine($"  oauth_token: {token}");

            if (string.IsNullOrWhiteSpace(existing))
            {
                await File.WriteAllTextAsync(path, hostBlock.ToString()).ConfigureAwait(false);
                return;
            }

            // Try to replace existing github.com block
            var lines = existing.Replace("\r\n", "\n").Split('\n');
            var outSb = new StringBuilder();
            bool inGithubBlock = false;
            bool replaced = false;
            foreach (var line in lines)
            {
                var trimmed = line.TrimStart();
                if (!inGithubBlock && trimmed.StartsWith("github.com:", StringComparison.Ordinal))
                {
                    // begin replace
                    outSb.Append(hostBlock);
                    inGithubBlock = true;
                    replaced = true;
                    continue;
                }

                if (inGithubBlock)
                {
                    // If this line is another top-level key (no leading spaces), we left the block
                    if (!line.StartsWith(" ") && !string.IsNullOrWhiteSpace(line))
                    {
                        inGithubBlock = false;
                    }
                    else
                    {
                        // skip existing block lines
                        continue;
                    }
                }

                outSb.AppendLine(line);
            }

            if (!replaced)
            {
                // append
                if (outSb.Length > 0 && !outSb.ToString().EndsWith("\n"))
                {
                    outSb.AppendLine();
                }

                outSb.Append(hostBlock);
            }

            await File.WriteAllTextAsync(path, outSb.ToString()).ConfigureAwait(false);
        }
    }
}
