namespace Synaxis.Application.Routing;

public class RequiredCapabilities
{
    public bool Streaming { get; set; }
    public bool Tools { get; set; }
    public bool Vision { get; set; }
    public bool StructuredOutput { get; set; }
    public bool LogProbs { get; set; }
    public static RequiredCapabilities Default => new();
}
