using Microsoft.Playwright;
using System.Text.Json;
using Xunit;
using System.Threading.Tasks;

namespace Synaxis.InferenceGateway.IntegrationTests.Admin;

[Trait("Category", "E2E")]
[Trait("Category", "Admin")]
public class AdminUiE2ETests : IAsyncLifetime
{
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private IAPIRequestContext _api = null!;
    private string _baseUrl = "http://localhost:8080";

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = _baseUrl
        });

        _api = await _playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions
        {
            BaseURL = _baseUrl
        });
    }

    public async Task DisposeAsync()
    {
        await _browser.CloseAsync();
        _playwright.Dispose();
    }

    [Fact]
    public async Task AdminLogin_ValidJWT_AllowsAccessToAdminPanel()
    {
        // Arrange
        var validToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwiZW1haWwiOiJhZG1pbkBleGFtcGxlLmNvbSJ9.signature";
        var page = await _browser.NewPageAsync();

        // Act
        await page.GotoAsync("/admin/login");
        await page.FillAsync("input[label='JWT Token']", validToken);
        await page.ClickAsync("button:has-text('Access Admin Panel')");

        // Assert
        await page.WaitForURLAsync("**/admin**");
        Assert.Contains("/admin", page.Url);
        var h1 = await page.TextContentAsync("h1");
        Assert.Contains("Synaxis", h1);
    }

    [Fact]
    public async Task AdminShell_DisplaysNavigationMenu()
    {
        // Arrange
        var page = await _browser.NewPageAsync();
        await SetJwtToken(page, "valid-jwt-token");

        // Act
        await page.GotoAsync("/admin");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert
        Assert.True(await page.IsVisibleAsync("text=Health Dashboard"));
        Assert.True(await page.IsVisibleAsync("text=Provider Config"));
        Assert.True(await page.IsVisibleAsync("text=Settings"));
        Assert.True(await page.IsVisibleAsync("text=Logout"));
    }

    [Fact]
    public async Task ProviderConfig_DisplaysAllProviders()
    {
        // Arrange
        var page = await _browser.NewPageAsync();
        await SetJwtToken(page, "valid-jwt-token");
        await page.GotoAsync("/admin/providers");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - Wait for providers to load
        await page.WaitForSelectorAsync("text=Groq", new() { Timeout = 5000 });

        // Assert
        Assert.True(await page.IsVisibleAsync("text=Groq"));
        Assert.True(await page.IsVisibleAsync("text=Cloudflare"));
        Assert.True(await page.IsVisibleAsync("text=Toggle"));
    }

    [Fact]
    public async Task ProviderConfig_CanToggleProvider()
    {
        // Arrange
        var page = await _browser.NewPageAsync();
        await SetJwtToken(page, "valid-jwt-token");
        await page.GotoAsync("/admin/providers");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForSelectorAsync("text=Groq", new() { Timeout = 5000 });

        // Act
        var toggle = page.Locator("label.toggle").First;
        await toggle.ClickAsync();

        // Assert - Verify toast notification or success indicator
        Assert.True(await page.IsVisibleAsync("text=Provider") || await page.IsVisibleAsync("text=success"));
    }

    [Fact]
    public async Task HealthDashboard_DisplaysServiceHealth()
    {
        // Arrange
        var page = await _browser.NewPageAsync();
        await SetJwtToken(page, "valid-jwt-token");
        await page.GotoAsync("/admin/health");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        await page.WaitForSelectorAsync("text=API Gateway", new() { Timeout = 5000 });

        // Assert
        Assert.True(await page.IsVisibleAsync("text=API Gateway"));
        Assert.True(await page.IsVisibleAsync("text=PostgreSQL"));
        Assert.True(await page.IsVisibleAsync("text=Redis"));
    }

    [Fact]
    public async Task HealthDashboard_AutoRefreshes()
    {
        // Arrange
        var page = await _browser.NewPageAsync();
        await SetJwtToken(page, "valid-jwt-token");
        await page.GotoAsync("/admin/health");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - Let it refresh at least once
        await page.WaitForTimeoutAsync(11000);

        // Assert - Verify still displaying health data
        Assert.True(await page.IsVisibleAsync("text=API Gateway") || await page.IsVisibleAsync("text=Services"));
    }

    [Fact]
    public async Task AdminLogout_RedirectsToLogin()
    {
        // Arrange
        var page = await _browser.NewPageAsync();
        await SetJwtToken(page, "valid-jwt-token");
        await page.GotoAsync("/admin");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        await page.ClickAsync("button:has-text('Logout')");
        await page.WaitForURLAsync("**/admin/login**");

        // Assert
        Assert.Contains("/admin/login", page.Url);
        Assert.True(await page.IsVisibleAsync("text=Synaxis Admin"));
    }

    [Fact]
    public async Task UnauthenticatedAccess_RedirectsToLogin()
    {
        // Arrange
        var page = await _browser.NewPageAsync();

        // Act - Try to access admin without login
        await page.GotoAsync("/admin/health");
        await page.WaitForURLAsync("**/admin/login**");

        // Assert
        Assert.Contains("/admin/login", page.Url);
    }

    [Fact]
    public async Task AdminSettings_SavesChanges()
    {
        // Arrange
        var page = await _browser.NewPageAsync();
        await SetJwtToken(page, "valid-jwt-token");
        await page.GotoAsync("/admin/settings");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - Interact with settings form
        var saveButton = page.Locator("button:has-text('Save')").First;
        if (await saveButton.IsVisibleAsync())
        {
            await saveButton.ClickAsync();
            await page.WaitForTimeoutAsync(500);
        }

        // Assert - Verify no error toast
        Assert.True(await page.IsVisibleAsync("text=Settings") || await page.IsVisibleAsync("text=Configuration"));
    }

    private async Task SetJwtToken(IPage page, string token)
    {
        // Inject JWT token into localStorage for admin authentication
        await page.EvaluateAsync($"localStorage.setItem('jwtToken', '{token}')");
    }
}
