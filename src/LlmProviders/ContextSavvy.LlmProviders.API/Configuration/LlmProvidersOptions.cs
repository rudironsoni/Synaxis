namespace ContextSavvy.LlmProviders.API.Configuration;

public class LlmProvidersOptions
{
    public const string SectionName = "LlmProviders";

    public string DefaultModel { get; set; } = "gpt-4o";
    public int MaxContextTokens { get; set; } = 128000;
    public bool EnableFallback { get; set; } = true;
    public double DefaultTemperature { get; set; } = 0.7;
}
