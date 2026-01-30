import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import MessageBubble from './MessageBubble';

describe('MessageBubble', () => {
  it('should render user message with correct styling', () => {
    render(<MessageBubble role="user" content="Hello from user" />);

    expect(screen.getByText('Hello from user')).toBeInTheDocument();
    const messageContainer = screen.getByText('Hello from user').parentElement;
    expect(messageContainer?.parentElement).toHaveClass('flex', 'justify-end');
  });

  it('should render assistant message with correct styling', () => {
    render(<MessageBubble role="assistant" content="Hello from assistant" />);

    expect(screen.getByText('Hello from assistant')).toBeInTheDocument();
    const messageContainer = screen.getByText('Hello from assistant').parentElement;
    expect(messageContainer?.parentElement).toHaveClass('flex', 'justify-start');
  });

  it('should render system message with assistant styling', () => {
    render(<MessageBubble role="system" content="System message" />);

    expect(screen.getByText('System message')).toBeInTheDocument();
    const messageContainer = screen.getByText('System message').parentElement;
    expect(messageContainer?.parentElement).toHaveClass('flex', 'justify-start');
  });

  it('should render message with usage information', () => {
    const usage = { prompt: 10, completion: 5, total: 15 };
    render(<MessageBubble role="assistant" content="Response with tokens" usage={usage} />);

    expect(screen.getByText('Response with tokens')).toBeInTheDocument();
    expect(screen.getByText(/Tokens: 15/)).toBeInTheDocument();
    expect(screen.getByText(/p:10/)).toBeInTheDocument();
    expect(screen.getByText(/c:5/)).toBeInTheDocument();
  });

  it('should not render usage section when usage is undefined', () => {
    render(<MessageBubble role="assistant" content="No usage info" />);

    expect(screen.getByText('No usage info')).toBeInTheDocument();
    expect(screen.queryByText(/Tokens:/)).not.toBeInTheDocument();
  });

  it('should preserve whitespace in message content', () => {
    const multilineContent = 'Line 1\nLine 2\nLine 3';
    render(<MessageBubble role="user" content={multilineContent} />);

    const contentElement = screen.getByText((text) => text.includes('Line 1') && text.includes('Line 2'));
    expect(contentElement).toHaveClass('whitespace-pre-wrap');
  });

  it('should handle long content', () => {
    const longContent = 'a'.repeat(1000);
    render(<MessageBubble role="assistant" content={longContent} />);

    expect(screen.getByText(longContent)).toBeInTheDocument();
  });

  it('should render special characters correctly', () => {
    const specialContent = '<script>alert("xss")</script> & more';
    render(<MessageBubble role="user" content={specialContent} />);

    expect(screen.getByText(specialContent)).toBeInTheDocument();
  });

  it('should render code blocks in content', () => {
    const codeContent = '```\nconst x = 1;\n```';
    render(<MessageBubble role="assistant" content={codeContent} />);

    expect(screen.getByText((text) => text.includes('const x = 1;'))).toBeInTheDocument();
  });

  it('should apply max-width constraint to message bubble', () => {
    render(<MessageBubble role="user" content="Test" />);

    const bubbleContent = screen.getByText('Test').parentElement;
    expect(bubbleContent).toHaveClass('max-w-[70%]');
  });

  it('should render usage with zero tokens', () => {
    const usage = { prompt: 0, completion: 0, total: 0 };
    render(<MessageBubble role="assistant" content="Zero tokens" usage={usage} />);

    expect(screen.getByText(/Tokens: 0/)).toBeInTheDocument();
    expect(screen.getByText(/p:0/)).toBeInTheDocument();
    expect(screen.getByText(/c:0/)).toBeInTheDocument();
  });
});
