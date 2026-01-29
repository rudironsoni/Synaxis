import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import App from '../App';
import { db } from '@/db/db';

// 1. Hoist the mock function so it's available to vi.mock
const mocks = vi.hoisted(() => ({
  sendMessage: vi.fn(),
}));

// 2. Mock the API client using the hoisted function
vi.mock('@/api/client', () => ({
  defaultClient: {
    sendMessage: mocks.sendMessage,
    updateConfig: vi.fn(),
  },
}));

// 3. Mock Dexie (keep inline or simple)
vi.mock('@/db/db', () => {
  const mock = {
    sessions: {
      toArray: vi.fn().mockResolvedValue([]),
      add: vi.fn().mockResolvedValue(1),
      update: vi.fn().mockResolvedValue(1),
    },
    messages: {
      toArray: vi.fn().mockResolvedValue([]),
      where: () => ({
        equals: (val: any) => ({ toArray: vi.fn().mockResolvedValue([]) }),
        sortBy: vi.fn().mockResolvedValue([]),
      }),
      add: vi.fn().mockResolvedValue(1),
      bulkAdd: vi.fn().mockResolvedValue(1),
    },
    transaction: vi.fn((mode, tables, callback) => callback()),
  };

  return {
    default: mock,
    db: mock,
  };
});

// polyfill matchMedia used by AppShell
window.matchMedia = window.matchMedia || vi.fn(() => ({
  matches: false,
  addListener: vi.fn(),
  removeListener: vi.fn(),
  addEventListener: vi.fn(),
  removeEventListener: vi.fn(),
  dispatchEvent: vi.fn(),
}));

// Mock scrollIntoView (DOM method missing in JSDOM)
window.HTMLElement.prototype.scrollIntoView = vi.fn();

describe('Integration Flow', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('allows a user to start a chat and send a message', async () => {
    // Setup API mock response
    mocks.sendMessage.mockResolvedValue({
      id: 'msg_123',
      choices: [{ message: { role: 'assistant', content: 'Hello human!' } }],
      usage: { prompt_tokens: 5, completion_tokens: 5, total_tokens: 10 },
    });

    render(<App />);

    // 1. Click "New Chat" (SessionList might be visible or inside sidebar)
    // In mobile view sidebar might be hidden, but default desktop is open.
    // Let's assume desktop view or button is accessible.
    const newChatBtn = screen.getByRole('button', { name: /new chat/i });
    fireEvent.click(newChatBtn);

    // 2. Wait for ChatInput to appear and type in input
    const input = await screen.findByPlaceholderText(/Type a message.../i);
    fireEvent.change(input, { target: { value: 'Hello' } });

    // 3. Click Send
    const sendBtn = await screen.findByLabelText(/Send/i); // Ensure ChatInput has aria-label="Send" or similar
    fireEvent.click(sendBtn);

    // 4. Expect User Message
    await waitFor(() => {
      expect(screen.getByText('Hello')).toBeInTheDocument();
    });

    // 5. Expect API Call
    // sendMessage is called with the messages array; no second arg is used
    expect(mocks.sendMessage).toHaveBeenCalledWith(
      expect.arrayContaining([
        expect.objectContaining({ role: 'user', content: 'Hello' }),
      ])
    );

    // 6. Expect Assistant Response
    await waitFor(() => {
      expect(screen.getByText('Hello human!')).toBeInTheDocument();
    });
  });
});
