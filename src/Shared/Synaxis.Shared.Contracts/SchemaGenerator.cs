using System.Text.Json;
using System.Text.Json.Nodes;

namespace Synaxis.Shared.Contracts;

public class SchemaGenerator
{
    public string GenerateSchema<T>()
    {
        return JsonSerializer.Serialize(typeof(T));
    }

    public string GenerateSchema(Type type)
    {
        return JsonSerializer.Serialize(type);
    }
    
    public Dictionary<string, string> GenerateV2Schemas()
    {
        return new Dictionary<string, string>();
    }

    public Dictionary<string, string> GenerateV2Schemas(string path)
    {
        return new Dictionary<string, string>();
    }

    public Dictionary<string, string> GenerateV1Schemas(string path)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("Path cannot be empty", nameof(path));
        return new Dictionary<string, string>();
    }

    public Dictionary<string, string> GenerateV1Schemas(bool includeV2)
    {
        return new Dictionary<string, string>();
    }

    public Dictionary<string, string> GenerateV1Schemas()
    {
        return new Dictionary<string, string>();
    }
}
