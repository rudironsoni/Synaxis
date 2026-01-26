using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Synaxis.InferenceGateway.Application.Configuration;

namespace Synaxis.InferenceGateway.WebApi.Controllers.v1;

/// <summary>
/// Controller for OpenAI-compatible models listing.
/// </summary>
[ApiController]
[Route("v1/models")]
public class ModelsController : ControllerBase
{
    private readonly SynaxisConfiguration _config;

    public ModelsController(IOptions<SynaxisConfiguration> config)
    {
        _config = config.Value;
    }

    /// <summary>
    /// Lists the currently available models.
    /// </summary>
    /// <returns>A list of model objects.</returns>
    [HttpGet]
    public IActionResult List()
    {
        var created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "default" };

        foreach (var model in _config.CanonicalModels)
        {
            if (!string.IsNullOrWhiteSpace(model.Id)) ids.Add(model.Id);
        }

        foreach (var alias in _config.Aliases.Keys)
        {
            if (!string.IsNullOrWhiteSpace(alias)) ids.Add(alias);
        }

        var models = ids.Select(id => new
        {
            id,
            @object = "model",
            created,
            owned_by = "synaxis"
        });

        return Ok(new { @object = "list", data = models });
    }
}
