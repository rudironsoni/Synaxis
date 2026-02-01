import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import ChatWindow from './ChatWindow';

// Hoisted mocks
const mocks = vi.hoisted(() => ({
  sendMessage: vi.fn(),
  sendMessageStream: vi.fn(),
  addUsage: vi.fn(),
  messagesWhere: vi.fn().mockReturnValue({
    equals: vi.fn().mockReturnValue({
      toArray: vi.fn().mockResolvedValue([]),
    }),
  }),
  messagesAdd: vi.fn().mockResolvedValue(1),
  gatewayUrl: 'http://localhost:5000',
  streamingEnabled: false,
  setStreamingEnabled: vi.fn(),
}));

// Mock API client
vi.mock('@/api/client', () => ({
  defaultClient: {
    sendMessage: mocks.sendMessage,
    sendMessageStream: mocks.sendMessageStream,
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
  default: (selector: (s: { gatewayUrl: string; streamingEnabled: boolean; setStreamingEnabled: () => void }) => unknown) => selector({
    gatewayUrl: mocks.gatewayUrl,
    streamingEnabled: mocks.streamingEnabled,
    setStreamingEnabled: mocks.setStreamingEnabled,
  }),
}));

// Mock usage store
vi.mock('@/stores/usage', () => ({
  default: (selector: (s: { addUsage: () => void }) => unknown) => selector({ addUsage: mocks.addUsage }),
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

  describe('Streaming Mode', () => {
    beforeEach(() => {
      mocks.streamingEnabled = true;
    });

    it('should use streaming when enabled', async () => {
      mocks.messagesWhere.mockReturnValue({
        equals: vi.fn().mockReturnValue({
          toArray: vi.fn().mockResolvedValue([]),
        }),
      });

      const mockStream = async function* () {
        yield {
          id: 'stream-1',
          object: 'chat.completion.chunk',
          created: 1234567890,
          model: 'default',
          choices: [{ index: 0, delta: { content: 'Hello' }, finish_reason: null }],
        };
        yield {
          id: 'stream-1',
          object: 'chat.completion.chunk',
          created: 1234567890,
          model: 'default',
          choices: [{ index: 0, delta: { content: ' world' }, finish_reason: null }],
        };
        yield {
          id: 'stream-1',
          object: 'chat.completion.chunk',
          created: 1234567890,
          model: 'default',
          choices: [{ index: 0, delta: {}, finish_reason: 'stop' }],
        };
      };

      mocks.sendMessageStream.mockReturnValue(mockStream());

      render(<ChatWindow sessionId={1} />);

      const input = screen.getByPlaceholderText('Type a message...');
      fireEvent.change(input, { target: { value: 'Stream test' } });
      fireEvent.click(screen.getByLabelText('Send'));

      await waitFor(() => {
        expect(mocks.sendMessageStream).toHaveBeenCalled();
      });

      await waitFor(() => {
        expect(screen.getByText('Hello world')).toBeInTheDocument();
      }, { timeout: 2000 });
    });

    it('should disable input while streaming', async () => {
      mocks.messagesWhere.mockReturnValue({
        equals: vi.fn().mockReturnValue({
          toArray: vi.fn().mockResolvedValue([]),
        }),
      });

      const mockStream = async function* () {
        yield {
          id: 'stream-2',
          object: 'chat.completion.chunk',
          created: 1234567890,
          model: 'default',
          choices: [{ index: 0, delta: { content: 'Streaming' }, finish_reason: null }],
        };
        await new Promise(resolve => setTimeout(resolve, 100));
        yield {
          id: 'stream-2',
          object: 'chat.completion.chunk',
          created: 1234567890,
          model: 'default',
          choices: [{ index: 0, delta: {}, finish_reason: 'stop' }],
        };
      };

      mocks.sendMessageStream.mockReturnValue(mockStream());

      render(<ChatWindow sessionId={1} />);

      const input = screen.getByPlaceholderText('Type a message...');
      fireEvent.change(input, { target: { value: 'Test' } });
      fireEvent.click(screen.getByLabelText('Send'));

      await waitFor(() => {
        const disabledInput = screen.getByPlaceholderText('Waiting for response...');
        expect(disabledInput).toBeDisabled();
      });
    });

    it('should show streaming indicator', async () => {
      mocks.messagesWhere.mockReturnValue({
        equals: vi.fn().mockReturnValue({
          toArray: vi.fn().mockResolvedValue([]),
        }),
      });

      const mockStream = async function* () {
        yield {
          id: 'stream-3',
          object: 'chat.completion.chunk',
          created: 1234567890,
          model: 'default',
          choices: [{ index: 0, delta: { content: 'Typing' }, finish_reason: null }],
        };
        await new Promise(resolve => setTimeout(resolve, 50));
        yield {
          id: 'stream-3',
          object: 'chat.completion.chunk',
          created: 1234567890,
          model: 'default',
          choices: [{ index: 0, delta: {}, finish_reason: 'stop' }],
        };
      };

      mocks.sendMessageStream.mockReturnValue(mockStream());

      render(<ChatWindow sessionId={1} />);

      const input = screen.getByPlaceholderText('Type a message...');
      fireEvent.change(input, { target: { value: 'Test streaming' } });
      fireEvent.click(screen.getByLabelText('Send'));

      await waitFor(() => {
        expect(screen.getByText(/streaming/i)).toBeInTheDocument();
      });
    });

    it('should handle streaming errors gracefully', async () => {
      mocks.messagesWhere.mockReturnValue({
        equals: vi.fn().mockReturnValue({
          toArray: vi.fn().mockResolvedValue([]),
        }),
      });

      mocks.sendMessageStream.mockImplementation(async () => {
        throw new Error('Stream failed');
      });

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

    it('should save completed stream message to database', async () => {
      mocks.messagesWhere.mockReturnValue({
        equals: vi.fn().mockReturnValue({
          toArray: vi.fn().mockResolvedValue([]),
        }),
      });

      const mockStream = async function* () {
        yield {
          id: 'stream-4',
          object: 'chat.completion.chunk',
          created: 1234567890,
          model: 'default',
          choices: [{ index: 0, delta: { content: 'Complete' }, finish_reason: null }],
        };
        yield {
          id: 'stream-4',
          object: 'chat.completion.chunk',
          created: 1234567890,
          model: 'default',
          choices: [{ index: 0, delta: { content: ' message' }, finish_reason: null }],
        };
        yield {
          id: 'stream-4',
          object: 'chat.completion.chunk',
          created: 1234567890,
          model: 'default',
          choices: [{ index: 0, delta: {}, finish_reason: 'stop' }],
        };
      };

      mocks.sendMessageStream.mockReturnValue(mockStream());

      render(<ChatWindow sessionId={1} />);

      const input = screen.getByPlaceholderText('Type a message...');
      fireEvent.change(input, { target: { value: 'DB stream test' } });
      fireEvent.click(screen.getByLabelText('Send'));

      await waitFor(() => {
        expect(mocks.messagesAdd).toHaveBeenCalledWith(
          expect.objectContaining({
            sessionId: 1,
            role: 'assistant',
            content: 'Complete message',
          })
        );
      }, { timeout: 2000 });
    });
  });
});
