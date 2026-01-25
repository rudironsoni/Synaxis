namespace Synaxis.Application.Routing;

public record CanonicalModelId(string Provider, string ModelPath)
{
    public override string ToString() => $"{Provider}/{ModelPath}";

    public static CanonicalModelId Parse(string input)
    {
        var parts = input.Split('/', 2);
        return parts.Length == 2 ? new CanonicalModelId(parts[0], parts[1]) : new CanonicalModelId("unknown", input);
    }
}
