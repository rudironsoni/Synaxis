import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import ChatWindow from './ChatWindow';

// Hoisted mocks
const mocks = vi.hoisted(() => ({
  sendMessage: vi.fn(),
  addUsage: vi.fn(),
  messagesWhere: vi.fn().mockReturnValue({
    equals: vi.fn().mockReturnValue({
      toArray: vi.fn().mockResolvedValue([]),
    }),
  }),
  messagesAdd: vi.fn().mockResolvedValue(1),
  gatewayUrl: 'http://localhost:5000',
}));

// Mock API client
vi.mock('@/api/client', () => ({
  defaultClient: {
    sendMessage: mocks.sendMessage,
    updateConfig: vi.fn(),
  },
}));

// Mock database
vi.mock('@/db/db', () => ({
  default: {
    messages: {
      where: mocks.messagesWhere,
      add: mocks.messagesAdd,
    },
  },
}));

// Mock settings store
vi.mock('@/stores/settings', () => ({
  default: (selector: any) => selector({ gatewayUrl: mocks.gatewayUrl }),
}));

// Mock usage store
vi.mock('@/stores/usage', () => ({
  default: (selector: any) => selector({ addUsage: mocks.addUsage }),
}));

describe('ChatWindow', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should render select a chat message when no sessionId', () => {
    render(<ChatWindow sessionId={undefined} />);

    expect(screen.getByText(/Select a chat/i)).toBeInTheDocument();
  });

  it('should render chat interface when sessionId is provided', async () => {
    mocks.messagesWhere.mockReturnValue({
      equals: vi.fn().mockReturnValue({
        toArray: vi.fn().mockResolvedValue([]),
      }),
    });

    render(<ChatWindow sessionId={1} />);

    expect(screen.getByPlaceholderText('Type a message...')).toBeInTheDocument();
    expect(screen.getByLabelText('Send')).toBeInTheDocument();
  });

  it('should display existing messages for session', async () => {
    const existingMessages = [
      { id: 1, sessionId: 1, role: 'user', content: 'Previous message', createdAt: new Date() },
      { id: 2, sessionId: 1, role: 'assistant', content: 'Previous response', createdAt: new Date() },
    ];

    mocks.messagesWhere.mockReturnValue({
      equals: vi.fn().mockReturnValue({
        toArray: vi.fn().mockResolvedValue(existingMessages),
      }),
    });

    render(<ChatWindow sessionId={1} />);

    await waitFor(() => {
      expect(screen.getByText('Previous message')).toBeInTheDocument();
      expect(screen.getByText('Previous response')).toBeInTheDocument();
    });
  });

  it('should send message and display user message immediately', async () => {
    mocks.messagesWhere.mockReturnValue({
      equals: vi.fn().mockReturnValue({
        toArray: vi.fn().mockResolvedValue([]),
      }),
    });

    mocks.sendMessage.mockResolvedValue({
      id: 'resp-123',
      choices: [{ message: { role: 'assistant', content: 'AI response' } }],
      usage: { prompt_tokens: 5, completion_tokens: 5, total_tokens: 10 },
    });

    render(<ChatWindow sessionId={1} />);

    const input = screen.getByPlaceholderText('Type a message...');
    fireEvent.change(input, { target: { value: 'Test message' } });
    fireEvent.click(screen.getByLabelText('Send'));

    await waitFor(() => {
      expect(screen.getByText('Test message')).toBeInTheDocument();
    });
  });

  it('should call API with correct message format', async () => {
    mocks.messagesWhere.mockReturnValue({
      equals: vi.fn().mockReturnValue({
        toArray: vi.fn().mockResolvedValue([]),
      }),
    });

    mocks.sendMessage.mockResolvedValue({
      id: 'resp-456',
      choices: [{ message: { role: 'assistant', content: 'Response' } }],
    });

    render(<ChatWindow sessionId={1} />);

    const input = screen.getByPlaceholderText('Type a message...');
    fireEvent.change(input, { target: { value: 'API test' } });
    fireEvent.click(screen.getByLabelText('Send'));

    await waitFor(() => {
      expect(mocks.sendMessage).toHaveBeenCalledWith(
        expect.arrayContaining([
          expect.objectContaining({ role: 'user', content: 'API test' }),
        ])
      );
    });
  });

  it('should display assistant response after API call', async () => {
    mocks.messagesWhere.mockReturnValue({
      equals: vi.fn().mockReturnValue({
        toArray: vi.fn().mockResolvedValue([]),
      }),
    });

    mocks.sendMessage.mockResolvedValue({
      id: 'resp-789',
      choices: [{ message: { role: 'assistant', content: 'Assistant reply' } }],
      usage: { prompt_tokens: 3, completion_tokens: 2, total_tokens: 5 },
    });

    render(<ChatWindow sessionId={1} />);

    const input = screen.getByPlaceholderText('Type a message...');
    fireEvent.change(input, { target: { value: 'Hello AI' } });
    fireEvent.click(screen.getByLabelText('Send'));

    await waitFor(() => {
      expect(screen.getByText('Assistant reply')).toBeInTheDocument();
    });
  });

  it('should track usage when response includes usage data', async () => {
    mocks.messagesWhere.mockReturnValue({
      equals: vi.fn().mockReturnValue({
        toArray: vi.fn().mockResolvedValue([]),
      }),
    });

    mocks.sendMessage.mockResolvedValue({
      id: 'resp-usage',
      choices: [{ message: { role: 'assistant', content: 'With usage' } }],
      usage: { prompt_tokens: 10, completion_tokens: 20, total_tokens: 30 },
    });

    render(<ChatWindow sessionId={1} />);

    const input = screen.getByPlaceholderText('Type a message...');
    fireEvent.change(input, { target: { value: 'Track usage' } });
    fireEvent.click(screen.getByLabelText('Send'));

    await waitFor(() => {
      expect(mocks.addUsage).toHaveBeenCalledWith(30);
    });
  });

  it('should handle API error gracefully', async () => {
    mocks.messagesWhere.mockReturnValue({
      equals: vi.fn().mockReturnValue({
        toArray: vi.fn().mockResolvedValue([]),
      }),
    });

    const errorMessage = 'Network error occurred';
    mocks.sendMessage.mockRejectedValue(new Error(errorMessage));

    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {});

    render(<ChatWindow sessionId={1} />);

    const input = screen.getByPlaceholderText('Type a message...');
    fireEvent.change(input, { target: { value: 'Error test' } });
    fireEvent.click(screen.getByLabelText('Send'));

    await waitFor(() => {
      expect(alertSpy).toHaveBeenCalledWith(expect.stringContaining('Failed to send message'));
    });

    alertSpy.mockRestore();
  });

  it('should handle response without usage data', async () => {
    mocks.messagesWhere.mockReturnValue({
      equals: vi.fn().mockReturnValue({
        toArray: vi.fn().mockResolvedValue([]),
      }),
    });

    mocks.sendMessage.mockResolvedValue({
      id: 'resp-no-usage',
      choices: [{ message: { role: 'assistant', content: 'No usage data' } }],
      // No usage field
    });

    render(<ChatWindow sessionId={1} />);

    const input = screen.getByPlaceholderText('Type a message...');
    fireEvent.change(input, { target: { value: 'Test no usage' } });
    fireEvent.click(screen.getByLabelText('Send'));

    await waitFor(() => {
      expect(screen.getByText('No usage data')).toBeInTheDocument();
    });

    // Should not call addUsage when no usage data
    expect(mocks.addUsage).not.toHaveBeenCalled();
  });

  it('should handle response with missing choices', async () => {
    mocks.messagesWhere.mockReturnValue({
      equals: vi.fn().mockReturnValue({
        toArray: vi.fn().mockResolvedValue([]),
      }),
    });

    mocks.sendMessage.mockResolvedValue({
      id: 'resp-empty',
      choices: [],
    });

    render(<ChatWindow sessionId={1} />);

    const input = screen.getByPlaceholderText('Type a message...');
    fireEvent.change(input, { target: { value: 'Empty response' } });
    fireEvent.click(screen.getByLabelText('Send'));

    await waitFor(() => {
      expect(screen.getByText('No response')).toBeInTheDocument();
    });
  });

  it('should save messages to database', async () => {
    mocks.messagesWhere.mockReturnValue({
      equals: vi.fn().mockReturnValue({
        toArray: vi.fn().mockResolvedValue([]),
      }),
    });

    mocks.sendMessage.mockResolvedValue({
      id: 'resp-db',
      choices: [{ message: { role: 'assistant', content: 'DB response' } }],
    });

    render(<ChatWindow sessionId={1} />);

    const input = screen.getByPlaceholderText('Type a message...');
    fireEvent.change(input, { target: { value: 'DB test' } });
    fireEvent.click(screen.getByLabelText('Send'));

    await waitFor(() => {
      // Should save user message
      expect(mocks.messagesAdd).toHaveBeenCalledWith(
        expect.objectContaining({
          sessionId: 1,
          role: 'user',
          content: 'DB test',
        })
      );

      // Should save assistant message
      expect(mocks.messagesAdd).toHaveBeenCalledWith(
        expect.objectContaining({
          sessionId: 1,
          role: 'assistant',
          content: 'DB response',
        })
      );
    });
  });

  it('should clear input after sending', async () => {
    mocks.messagesWhere.mockReturnValue({
      equals: vi.fn().mockReturnValue({
        toArray: vi.fn().mockResolvedValue([]),
      }),
    });

    mocks.sendMessage.mockResolvedValue({
      id: 'resp-clear',
      choices: [{ message: { role: 'assistant', content: 'Cleared' } }],
    });

    render(<ChatWindow sessionId={1} />);

    const input = screen.getByPlaceholderText('Type a message...') as HTMLTextAreaElement;
    fireEvent.change(input, { target: { value: 'Clear me' } });
    fireEvent.click(screen.getByLabelText('Send'));

    await waitFor(() => {
      expect(input.value).toBe('');
    });
  });
});
