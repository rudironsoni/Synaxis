import { test, expect } from '@playwright/test';

/**
 * Admin UI E2E Tests
 *
 * Tests for:
 * - Login flow with JWT authentication
 * - Provider configuration (enable/disable, edit)
 * - Health dashboard verification
 *
 * Note: These tests use the dev-login endpoint for testing purposes.
 * In production, use proper JWT authentication flow.
 */

test.describe('Admin UI - Login Flow', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/admin/login');
  });

  test('should display login page with correct elements', async ({ page }) => {
    await expect(page.locator('h1:has-text("Synaxis Admin")')).toBeVisible();

    const tokenInput = page.locator('input#token');
    await expect(tokenInput).toBeVisible();
    await expect(tokenInput).toHaveAttribute('type', 'password');
    await expect(tokenInput).toHaveAttribute('placeholder', 'eyJhbGciOiJIUzI1NiIs...');

    const submitButton = page.locator('button:has-text("Access Admin Panel")');
    await expect(submitButton).toBeVisible();

    const backLink = page.locator('a:has-text("Back to Chat")');
    await expect(backLink).toBeVisible();
    await expect(backLink).toHaveAttribute('href', '/');
  });

  test('should show error when token is empty', async ({ page }) => {
    const submitButton = page.locator('button:has-text("Access Admin Panel")');
    await submitButton.click();

    const errorMessage = page.locator('p:has-text("Please enter a JWT token")');
    await expect(errorMessage).toBeVisible();
  });

  test('should show error when token format is invalid', async ({ page }) => {
    const tokenInput = page.locator('input#token');
    await tokenInput.fill('invalid-token');

    const submitButton = page.locator('button:has-text("Access Admin Panel")');
    await submitButton.click();

    const errorMessage = page.locator('p:has-text("Invalid JWT token format")');
    await expect(errorMessage).toBeVisible();
  });

  test('should toggle token visibility', async ({ page }) => {
    const tokenInput = page.locator('input#token');
    const toggleButton = page.locator('button[aria-label="Show token"]');

    await expect(tokenInput).toHaveAttribute('type', 'password');
    await expect(toggleButton).toBeVisible();

    await toggleButton.click();
    await expect(tokenInput).toHaveAttribute('type', 'text');

    const hideButton = page.locator('button[aria-label="Hide token"]');
    await expect(hideButton).toBeVisible();

    await hideButton.click();
    await expect(tokenInput).toHaveAttribute('type', 'password');
  });

  test('should login with valid JWT token and redirect to admin dashboard', async ({ page }) => {
    const loginResponse = await page.request.post('/auth/dev-login', {
      headers: {
        'Content-Type': 'application/json',
      },
      data: {
        email: 'admin@synaxis.dev',
      },
    });

    expect(loginResponse.ok()).toBeTruthy();
    const loginData = await loginResponse.json();
    expect(loginData.token).toBeDefined();

    const tokenInput = page.locator('input#token');
    await tokenInput.fill(loginData.token);

    const submitButton = page.locator('button:has-text("Access Admin Panel")');
    await submitButton.click();

    await page.waitForURL('/admin');
    await expect(page).toHaveURL('/admin');

    await expect(page.locator('h1:has-text("Synaxis")')).toBeVisible();
    await expect(page.locator('p:has-text("Admin Panel")')).toBeVisible();
  });
});

test.describe('Admin UI - Provider Configuration', () => {
  let authToken: string;

  test.beforeEach(async ({ page }) => {
    const loginResponse = await page.request.post('/auth/dev-login', {
      headers: {
        'Content-Type': 'application/json',
      },
      data: {
        email: 'admin@synaxis.dev',
      },
    });

    const loginData = await loginResponse.json();
    authToken = loginData.token;

    await page.goto('/admin/login');
    await page.evaluate((token) => {
      localStorage.setItem('jwtToken', token);
    }, authToken);

    await page.goto('/admin/providers');
    await page.waitForLoadState('networkidle');
  });

  test('should display provider configuration page', async ({ page }) => {
    await expect(page.locator('h3:has-text("Provider Configuration")')).toBeVisible();

    await expect(page.locator('p:has-text("Manage AI provider settings")')).toBeVisible();

    const refreshButton = page.locator('button:has-text("Refresh")');
    await expect(refreshButton).toBeVisible();
  });

  test('should display provider cards', async ({ page }) => {
    await page.waitForSelector('[data-testid="provider-card"]', { timeout: 5000 });

    const providerCards = page.locator('[data-testid="provider-card"]');
    const count = await providerCards.count();
    expect(count).toBeGreaterThan(0);

    const firstCard = providerCards.first();
    await expect(firstCard.locator('h4')).toBeVisible();
    await expect(firstCard.locator('text=Tier')).toBeVisible();
  });

  test('should expand provider card to show details', async ({ page }) => {
    await page.waitForSelector('[data-testid="provider-card"]', { timeout: 5000 });

    const firstCard = page.locator('[data-testid="provider-card"]').first();

    await firstCard.click();

    await page.waitForTimeout(500);

    await expect(firstCard.locator('label:has-text("Provider ID")')).toBeVisible();
    await expect(firstCard.locator('label:has-text("Type")')).toBeVisible();
    await expect(firstCard.locator('label:has-text("API Key")')).toBeVisible();
    await expect(firstCard.locator('label:has-text("Available Models")')).toBeVisible();
  });

  test('should toggle provider enabled/disabled state', async ({ page }) => {
    await page.waitForSelector('[data-testid="provider-card"]', { timeout: 5000 });

    const firstCard = page.locator('[data-testid="provider-card"]').first();

    const toggleButton = firstCard.locator('button:has-text("Enabled"), button:has-text("Disabled")');
    await expect(toggleButton).toBeVisible();

    const initialText = await toggleButton.textContent();
    const initialState = initialText?.includes('Enabled') ? 'enabled' : 'disabled';

    await toggleButton.click();

    await page.waitForTimeout(1000);

    const newText = await toggleButton.textContent();
    const newState = newText?.includes('Enabled') ? 'enabled' : 'disabled';

    expect(newState).not.toBe(initialState);

    await toggleButton.click();
    await page.waitForTimeout(1000);

    const finalText = await toggleButton.textContent();
    const finalState = finalText?.includes('Enabled') ? 'enabled' : 'disabled';

    expect(finalState).toBe(initialState);
  });

  test('should show provider status indicators', async ({ page }) => {
    await page.waitForSelector('[data-testid="provider-card"]', { timeout: 5000 });

    const firstCard = page.locator('[data-testid="provider-card"]').first();

    const statusIcon = firstCard.locator('svg').first();
    await expect(statusIcon).toBeVisible();

    const statusText = firstCard.locator('text=Online, text=Offline, text=Unknown');
    await expect(statusText).toBeVisible();
  });

  test('should show API key status', async ({ page }) => {
    await page.waitForSelector('[data-testid="provider-card"]', { timeout: 5000 });

    const firstCard = page.locator('[data-testid="provider-card"]').first();

    const keyStatus = firstCard.locator('text=Key set, text=No key');
    await expect(keyStatus).toBeVisible();
  });

  test('should allow editing API key', async ({ page }) => {
    await page.waitForSelector('[data-testid="provider-card"]', { timeout: 5000 });

    const firstCard = page.locator('[data-testid="provider-card"]').first();

    await firstCard.click();
    await page.waitForTimeout(500);

    const setKeyButton = firstCard.locator('button:has-text("Set Key"), button:has-text("Update Key")');
    await expect(setKeyButton).toBeVisible();
    await setKeyButton.click();

    const keyInput = firstCard.locator('input[type="password"]');
    await expect(keyInput).toBeVisible();

    const saveButton = firstCard.locator('button:has-text("Save")');
    await expect(saveButton).toBeVisible();

    const cancelButton = firstCard.locator('button:has-text("Cancel")');
    await expect(cancelButton).toBeVisible();

    await cancelButton.click();
    await page.waitForTimeout(500);

    await expect(setKeyButton).toBeVisible();
  });

  test('should display provider models', async ({ page }) => {
    await page.waitForSelector('[data-testid="provider-card"]', { timeout: 5000 });

    const firstCard = page.locator('[data-testid="provider-card"]').first();

    await firstCard.click();
    await page.waitForTimeout(500);

    await expect(firstCard.locator('label:has-text("Available Models")')).toBeVisible();

    const models = firstCard.locator('[data-testid="provider-model"]');
    const modelCount = await models.count();
    expect(modelCount).toBeGreaterThan(0);
  });

  test('should refresh provider list', async ({ page }) => {
    await page.waitForSelector('[data-testid="provider-card"]', { timeout: 5000 });

    const refreshButton = page.locator('button:has-text("Refresh")');
    await expect(refreshButton).toBeVisible();

    await refreshButton.click();

    await page.waitForTimeout(1000);

    const providerCards = page.locator('[data-testid="provider-card"]');
    const count = await providerCards.count();
    expect(count).toBeGreaterThan(0);
  });
});

test.describe('Admin UI - Health Dashboard', () => {
  let authToken: string;

  test.beforeEach(async ({ page }) => {
    const loginResponse = await page.request.post('/auth/dev-login', {
      headers: {
        'Content-Type': 'application/json',
      },
      data: {
        email: 'admin@synaxis.dev',
      },
    });

    const loginData = await loginResponse.json();
    authToken = loginData.token;

    await page.goto('/admin/login');
    await page.evaluate((token) => {
      localStorage.setItem('jwtToken', token);
    }, authToken);

    await page.goto('/admin/health');
    await page.waitForLoadState('networkidle');
  });

  test('should display health dashboard page', async ({ page }) => {
    await expect(page.locator('h3:has-text("Health Dashboard")')).toBeVisible();

    await expect(page.locator('p:has-text("Monitor the status of all providers")')).toBeVisible();

    const refreshButton = page.locator('button:has-text("Refresh")');
    await expect(refreshButton).toBeVisible();
  });

  test('should display overall system status', async ({ page }) => {
    await page.waitForSelector('[data-testid="overall-status"]', { timeout: 5000 });

    const overallStatus = page.locator('[data-testid="overall-status"]');
    await expect(overallStatus).toBeVisible();

    await expect(overallStatus.locator('h4:has-text("Overall System Status")')).toBeVisible();

    const statusDescription = overallStatus.locator('p');
    await expect(statusDescription).toBeVisible();
  });

  test('should display services section', async ({ page }) => {
    await page.waitForSelector('[data-testid="services-section"]', { timeout: 5000 });

    await expect(page.locator('h4:has-text("Services")')).toBeVisible();

    const serviceCards = page.locator('[data-testid="service-card"]');
    const count = await serviceCards.count();
    expect(count).toBeGreaterThan(0);

    const firstService = serviceCards.first();
    await expect(firstService.locator('p:has-text("Last checked")')).toBeVisible();
  });

  test('should display AI providers section', async ({ page }) => {
    await page.waitForSelector('[data-testid="providers-section"]', { timeout: 5000 });

    await expect(page.locator('h4:has-text("AI Providers")')).toBeVisible();

    const providerCards = page.locator('[data-testid="health-provider-card"]');
    const count = await providerCards.count();
    expect(count).toBeGreaterThan(0);

    const firstProvider = providerCards.first();
    await expect(firstProvider.locator('p:has-text("Last checked")')).toBeVisible();
  });

  test('should display provider latency information', async ({ page }) => {
    await page.waitForSelector('[data-testid="health-provider-card"]', { timeout: 5000 });

    const firstProvider = page.locator('[data-testid="health-provider-card"]').first();

    const latencyText = firstProvider.locator('text=/\\d+ms/');
    const hasLatency = await latencyText.count() > 0;

    if (hasLatency) {
      await expect(latencyText.first()).toBeVisible();
    }
  });

  test('should display provider success rate', async ({ page }) => {
    await page.waitForSelector('[data-testid="health-provider-card"]', { timeout: 5000 });

    const firstProvider = page.locator('[data-testid="health-provider-card"]').first();

    const successRateText = firstProvider.locator('text=Success Rate');
    const hasSuccessRate = await successRateText.count() > 0;

    if (hasSuccessRate) {
      await expect(successRateText).toBeVisible();

      const progressBar = firstProvider.locator('[role="progressbar"], div[class*="h-full"]');
      await expect(progressBar).toBeVisible();
    }
  });

  test('should display provider error messages when applicable', async ({ page }) => {
    await page.waitForSelector('[data-testid="health-provider-card"]', { timeout: 5000 });

    const errorMessages = page.locator('[data-testid="health-provider-card"] p:has-text("timeout"), [data-testid="health-provider-card"] p:has-text("error")');
    const errorCount = await errorMessages.count();

    if (errorCount > 0) {
      await expect(errorMessages.first()).toBeVisible();
    }
  });

  test('should toggle auto-refresh', async ({ page }) => {
    await page.waitForSelector('[data-testid="overall-status"]', { timeout: 5000 });

    const autoRefreshButton = page.locator('button:has-text("Auto"), button:has-text("Paused")');
    await expect(autoRefreshButton).toBeVisible();

    const initialText = await autoRefreshButton.textContent();
    const initialState = initialText?.includes('Auto') ? 'auto' : 'paused';

    await autoRefreshButton.click();

    await page.waitForTimeout(500);

    const newText = await autoRefreshButton.textContent();
    const newState = newText?.includes('Auto') ? 'auto' : 'paused';

    expect(newState).not.toBe(initialState);

    await autoRefreshButton.click();
    await page.waitForTimeout(500);

    const finalText = await autoRefreshButton.textContent();
    const finalState = finalText?.includes('Auto') ? 'auto' : 'paused';

    expect(finalState).toBe(initialState);
  });

  test('should refresh health data', async ({ page }) => {
    await page.waitForSelector('[data-testid="overall-status"]', { timeout: 5000 });

    const refreshButton = page.locator('button:has-text("Refresh")');
    await expect(refreshButton).toBeVisible();

    await refreshButton.click();

    await page.waitForTimeout(1000);

    await expect(page.locator('[data-testid="overall-status"]')).toBeVisible();
  });

  test('should display last updated timestamp', async ({ page }) => {
    await page.waitForSelector('[data-testid="overall-status"]', { timeout: 5000 });

    const lastUpdated = page.locator('text=Last updated');
    await expect(lastUpdated).toBeVisible();
  });
});

test.describe('Admin UI - Navigation', () => {
  let authToken: string;

  test.beforeEach(async ({ page }) => {
    const loginResponse = await page.request.post('/auth/dev-login', {
      headers: {
        'Content-Type': 'application/json',
      },
      data: {
        email: 'admin@synaxis.dev',
      },
    });

    const loginData = await loginResponse.json();
    authToken = loginData.token;

    await page.goto('/admin/login');
    await page.evaluate((token) => {
      localStorage.setItem('jwtToken', token);
    }, authToken);

    await page.goto('/admin');
    await page.waitForLoadState('networkidle');
  });

  test('should display admin shell with sidebar navigation', async ({ page }) => {
    const sidebar = page.locator('aside');
    await expect(sidebar).toBeVisible();

    await expect(page.locator('a:has-text("Health Dashboard")')).toBeVisible();
    await expect(page.locator('a:has-text("Provider Config")')).toBeVisible();
    await expect(page.locator('a:has-text("Settings")')).toBeVisible();
  });

  test('should navigate to health dashboard', async ({ page }) => {
    const healthLink = page.locator('a:has-text("Health Dashboard")');
    await healthLink.click();

    await page.waitForURL('/admin/health');
    await expect(page).toHaveURL('/admin/health');

    await expect(page.locator('h3:has-text("Health Dashboard")')).toBeVisible();
  });

  test('should navigate to provider configuration', async ({ page }) => {
    const providersLink = page.locator('a:has-text("Provider Config")');
    await providersLink.click();

    await page.waitForURL('/admin/providers');
    await expect(page).toHaveURL('/admin/providers');

    await expect(page.locator('h3:has-text("Provider Configuration")')).toBeVisible();
  });

  test('should navigate to settings', async ({ page }) => {
    const settingsLink = page.locator('a:has-text("Settings")');
    await settingsLink.click();

    await page.waitForURL('/admin/settings');
    await expect(page).toHaveURL('/admin/settings');
  });

  test('should logout and redirect to login page', async ({ page }) => {
    const logoutButton = page.locator('button:has-text("Logout")');
    await logoutButton.click();

    await page.waitForURL('/admin/login');
    await expect(page).toHaveURL('/admin/login');

    const jwtToken = await page.evaluate(() => localStorage.getItem('jwtToken'));
    expect(jwtToken).toBeNull();
  });

  test('should highlight active navigation link', async ({ page }) => {
    const healthLink = page.locator('a:has-text("Health Dashboard")');
    await healthLink.click();
    await page.waitForURL('/admin/health');

    await expect(healthLink).toHaveClass(/bg-\[var\(--primary\)\]\/10/);

    const providersLink = page.locator('a:has-text("Provider Config")');
    await providersLink.click();
    await page.waitForURL('/admin/providers');

    await expect(providersLink).toHaveClass(/bg-\[var\(--primary\)\]\/10/);
    await expect(healthLink).not.toHaveClass(/bg-\[var\(--primary\)\]\/10/);
  });
});
