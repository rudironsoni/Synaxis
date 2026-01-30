import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import ChatInput from './ChatInput';

describe('ChatInput', () => {
  const mockOnSend = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should render textarea and send button', () => {
    render(<ChatInput onSend={mockOnSend} />);

    expect(screen.getByPlaceholderText('Type a message...')).toBeInTheDocument();
    expect(screen.getByLabelText('Send')).toBeInTheDocument();
  });

  it('should update textarea value on type', () => {
    render(<ChatInput onSend={mockOnSend} />);
    const textarea = screen.getByPlaceholderText('Type a message...');

    fireEvent.change(textarea, { target: { value: 'Hello world' } });

    expect(textarea).toHaveValue('Hello world');
  });

  it('should call onSend when clicking send button', () => {
    render(<ChatInput onSend={mockOnSend} />);
    const textarea = screen.getByPlaceholderText('Type a message...');
    const sendButton = screen.getByLabelText('Send');

    fireEvent.change(textarea, { target: { value: 'Test message' } });
    fireEvent.click(sendButton);

    expect(mockOnSend).toHaveBeenCalledWith('Test message');
  });

  it('should call onSend when pressing Enter', () => {
    render(<ChatInput onSend={mockOnSend} />);
    const textarea = screen.getByPlaceholderText('Type a message...');

    fireEvent.change(textarea, { target: { value: 'Enter message' } });
    fireEvent.keyDown(textarea, { key: 'Enter', shiftKey: false });

    expect(mockOnSend).toHaveBeenCalledWith('Enter message');
  });

  it('should not call onSend when pressing Shift+Enter', () => {
    render(<ChatInput onSend={mockOnSend} />);
    const textarea = screen.getByPlaceholderText('Type a message...');

    fireEvent.change(textarea, { target: { value: 'Multi\nline' } });
    fireEvent.keyDown(textarea, { key: 'Enter', shiftKey: true });

    expect(mockOnSend).not.toHaveBeenCalled();
  });

  it('should clear textarea after sending', () => {
    render(<ChatInput onSend={mockOnSend} />);
    const textarea = screen.getByPlaceholderText('Type a message...');

    fireEvent.change(textarea, { target: { value: 'Message to send' } });
    fireEvent.click(screen.getByLabelText('Send'));

    expect(textarea).toHaveValue('');
  });

  it('should trim whitespace from message', () => {
    render(<ChatInput onSend={mockOnSend} />);
    const textarea = screen.getByPlaceholderText('Type a message...');

    fireEvent.change(textarea, { target: { value: '  trimmed message  ' } });
    fireEvent.click(screen.getByLabelText('Send'));

    expect(mockOnSend).toHaveBeenCalledWith('trimmed message');
  });

  it('should not call onSend for empty message', () => {
    render(<ChatInput onSend={mockOnSend} />);
    const sendButton = screen.getByLabelText('Send');

    fireEvent.click(sendButton);

    expect(mockOnSend).not.toHaveBeenCalled();
  });

  it('should not call onSend for whitespace-only message', () => {
    render(<ChatInput onSend={mockOnSend} />);
    const textarea = screen.getByPlaceholderText('Type a message...');

    fireEvent.change(textarea, { target: { value: '   ' } });
    fireEvent.click(screen.getByLabelText('Send'));

    expect(mockOnSend).not.toHaveBeenCalled();
  });

  it('should allow multi-line input with Shift+Enter', () => {
    render(<ChatInput onSend={mockOnSend} />);
    const textarea = screen.getByPlaceholderText('Type a message...');

    fireEvent.change(textarea, { target: { value: 'Line 1' } });
    fireEvent.keyDown(textarea, { key: 'Enter', shiftKey: true });
    fireEvent.change(textarea, { target: { value: 'Line 1\nLine 2' } });

    expect(textarea).toHaveValue('Line 1\nLine 2');
    expect(mockOnSend).not.toHaveBeenCalled();
  });

  it('should have correct accessibility attributes', () => {
    render(<ChatInput onSend={mockOnSend} />);

    expect(screen.getByPlaceholderText('Type a message...')).toHaveAttribute('rows', '1');
    expect(screen.getByLabelText('Send')).toHaveAttribute('aria-label', 'Send');
  });

  it('should handle rapid send clicks gracefully', () => {
    render(<ChatInput onSend={mockOnSend} />);
    const textarea = screen.getByPlaceholderText('Type a message...');
    const sendButton = screen.getByLabelText('Send');

    fireEvent.change(textarea, { target: { value: 'Message' } });
    fireEvent.click(sendButton);
    fireEvent.click(sendButton);
    fireEvent.click(sendButton);

    expect(mockOnSend).toHaveBeenCalledTimes(1);
    expect(mockOnSend).toHaveBeenCalledWith('Message');
  });
});
