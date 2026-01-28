using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Synaxis.InferenceGateway.Application.Configuration;

namespace Synaxis.InferenceGateway.Infrastructure.Auth;

public class FileTokenStore : ITokenStore
{
    private readonly string _path;
    private readonly ILogger<FileTokenStore> _logger;

    public FileTokenStore(string path, ILogger<FileTokenStore> logger)
    {
        _path = path ?? throw new ArgumentNullException(nameof(path));
        _logger = logger;
    }

    public async Task<List<AntigravityAccount>> LoadAsync()
    {
        if (!File.Exists(_path)) return new List<AntigravityAccount>();

        try
        {
            var json = await File.ReadAllTextAsync(_path);
            var accounts = JsonSerializer.Deserialize<List<AntigravityAccount>>(json);
            return accounts ?? new List<AntigravityAccount>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load accounts from {Path}", _path);
            return new List<AntigravityAccount>();
        }
    }

    public async Task SaveAsync(List<AntigravityAccount> accounts)
    {
        try
        {
            var dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(accounts);
            await File.WriteAllTextAsync(_path, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save accounts.");
        }
    }
}
