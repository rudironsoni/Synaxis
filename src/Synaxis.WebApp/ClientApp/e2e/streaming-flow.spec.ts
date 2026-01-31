import { test, expect } from '@playwright/test';

test.describe('Streaming Flow', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    await page.waitForSelector('text=Synaxis');
  });

  test('should display app shell with header and sidebar', async ({ page }) => {
    // Check for header
    const header = page.locator('header');
    await expect(header).toBeVisible();
    
    // Check for Synaxis title
    const title = page.locator('h2:has-text("Synaxis")');
    await expect(title).toBeVisible();
    
    // Check for sidebar
    const sidebar = page.locator('aside');
    await expect(sidebar).toBeVisible();
  });

  test('should create new chat session', async ({ page }) => {
    // Find the new chat button (plus icon)
    const newChatButton = page.locator('button[aria-label="New chat"]');
    await expect(newChatButton).toBeVisible();
    
    // Click to create new chat
    await newChatButton.click();
    
    // Wait for chat window to appear
    await page.waitForSelector('textarea[placeholder="Type a message..."]');
    
    // Check that chat input is visible
    const chatInput = page.locator('textarea[placeholder="Type a message..."]');
    await expect(chatInput).toBeVisible();
  });

  test('should display streaming toggle with correct initial state', async ({ page }) => {
    // Create a session first
    const newChatButton = page.locator('button[aria-label="New chat"]');
    await newChatButton.click();
    await page.waitForSelector('textarea[placeholder="Type a message..."]');
    
    // Find the streaming toggle button (it has the Zap icon)
    const streamingToggle = page.locator('button:has(span:text("Streaming"))');
    await expect(streamingToggle).toBeVisible();
    
    // Check that it shows either ON or OFF
    const toggleText = await streamingToggle.textContent();
    expect(toggleText).toMatch(/Streaming (ON|OFF)/);
  });

  test('should toggle streaming mode', async ({ page }) => {
    // Create a session first
    const newChatButton = page.locator('button[aria-label="New chat"]');
    await newChatButton.click();
    await page.waitForSelector('textarea[placeholder="Type a message..."]');
    
    // Find the streaming toggle button
    const streamingToggle = page.locator('button:has(span:text("Streaming"))');
    await expect(streamingToggle).toBeVisible();
    
    const initialText = await streamingToggle.textContent();
    const initialState = initialText?.includes('ON') ? 'ON' : 'OFF';
    
    // Click to toggle
    await streamingToggle.click();
    
    // Verify state changed
    const newText = await streamingToggle.textContent();
    const newState = newText?.includes('ON') ? 'ON' : 'OFF';
    
    expect(newState).not.toBe(initialState);
    
    // Toggle back
    await streamingToggle.click();
    
    // Verify state reverted
    const finalText = await streamingToggle.textContent();
    const finalState = finalText?.includes('ON') ? 'ON' : 'OFF';
    
    expect(finalState).toBe(initialState);
  });

  test('should have chat input and send button', async ({ page }) => {
    // Create a session first
    const newChatButton = page.locator('button[aria-label="New chat"]');
    await newChatButton.click();
    await page.waitForSelector('textarea[placeholder="Type a message..."]');
    
    // Check for chat input
    const chatInput = page.locator('textarea[placeholder="Type a message..."]');
    await expect(chatInput).toBeVisible();
    
    // Check for send button
    const sendButton = page.locator('button[aria-label="Send"]');
    await expect(sendButton).toBeVisible();
  });

  test('should display correct aria labels for streaming toggle', async ({ page }) => {
    // Create a session first
    const newChatButton = page.locator('button[aria-label="New chat"]');
    await newChatButton.click();
    await page.waitForSelector('textarea[placeholder="Type a message..."]');
    
    // Find the streaming toggle button
    const streamingToggle = page.locator('button:has(span:text("Streaming"))');
    await expect(streamingToggle).toBeVisible();
    
    const toggleText = await streamingToggle.textContent();
    
    if (toggleText?.includes('ON')) {
      await expect(streamingToggle).toHaveAttribute('aria-label', 'Disable streaming');
    } else {
      await expect(streamingToggle).toHaveAttribute('aria-label', 'Enable streaming');
    }
  });
});