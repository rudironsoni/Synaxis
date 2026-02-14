import { Message } from '../App'

interface MessageBubbleProps {
  message: Message
  theme: 'light' | 'dark'
  isStreaming?: boolean
  onCopy: (content: string) => void
  onRegenerate: () => void
}

export function MessageBubble({ message, theme, isStreaming, onCopy, onRegenerate }: MessageBubbleProps) {
  const isUser = message.role === 'user'

  return (
    <div
      style={{
        display: 'flex',
        marginBottom: '20px',
        justifyContent: isUser ? 'flex-end' : 'flex-start'
      }}
    >
      <div
        style={{
          maxWidth: '70%',
          padding: '12px 16px',
          backgroundColor: isUser
            ? (theme === 'dark' ? '#2563eb' : '#3b82f6')
            : (theme === 'dark' ? '#0d0d0d' : '#f5f5f5'),
          color: isUser ? '#ffffff' : (theme === 'dark' ? '#ffffff' : '#000000'),
          borderRadius: '12px',
          borderTopLeftRadius: isUser ? '12px' : '2px',
          borderTopRightRadius: isUser ? '2px' : '12px',
          position: 'relative'
        }}
      >
        <div style={{
          fontSize: '13px',
          fontWeight: '500',
          marginBottom: '8px',
          opacity: 0.7
        }}>
          {isUser ? '👤 You' : '🤖 Assistant'}
        </div>
        <div style={{
          fontSize: '14px',
          lineHeight: '1.6',
          whiteSpace: 'pre-wrap',
          wordBreak: 'break-word'
        }}>
          {message.content}
          {isStreaming && <span className="cursor">|</span>}
        </div>
        <div style={{
          marginTop: '8px',
          display: 'flex',
          gap: '8px',
          fontSize: '12px'
        }}>
          <button
            onClick={() => onCopy(message.content)}
            style={{
              backgroundColor: 'transparent',
              color: 'inherit',
              border: 'none',
              cursor: 'pointer',
              opacity: 0.6,
              padding: '2px 6px',
              borderRadius: '4px'
            }}
            title="Copy"
          >
            📋
          </button>
          {!isUser && !isStreaming && (
            <button
              onClick={onRegenerate}
              style={{
                backgroundColor: 'transparent',
                color: 'inherit',
                border: 'none',
                cursor: 'pointer',
                opacity: 0.6,
                padding: '2px 6px',
                borderRadius: '4px'
              }}
              title="Regenerate"
            >
              🔄
            </button>
          )}
        </div>
      </div>
    </div>
  )
}
