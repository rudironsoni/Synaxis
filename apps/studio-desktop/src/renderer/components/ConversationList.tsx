import React from 'react'
import { useStore } from '../App'

export function ConversationList() {
  const { conversations, currentConversation, setCurrentConversation, settings } = useStore()

  const handleSelectConversation = (id: string) => {
    const conversation = conversations.find(c => c.id === id)
    if (conversation) {
      setCurrentConversation(conversation)
    }
  }

  const handleDeleteConversation = async (e: React.MouseEvent, id: string) => {
    e.stopPropagation()
    await window.electronAPI.deleteConversation(id)
    const updated = conversations.filter(c => c.id !== id)
    useStore.getState().setConversations(updated)
    if (currentConversation?.id === id) {
      useStore.getState().setCurrentConversation(updated[0] || null)
    }
  }

  return (
    <div style={{
      flex: 1,
      overflowY: 'auto',
      padding: '8px'
    }}>
      {conversations.length === 0 ? (
        <div style={{
          padding: '16px',
          textAlign: 'center',
          fontSize: '12px',
          color: settings.theme === 'dark' ? '#666' : '#999'
        }}>
          No conversations yet
        </div>
      ) : (
        conversations.map((conversation) => (
          <div
            key={conversation.id}
            onClick={() => handleSelectConversation(conversation.id)}
            style={{
              padding: '12px',
              marginBottom: '4px',
              backgroundColor: currentConversation?.id === conversation.id
                ? (settings.theme === 'dark' ? '#2563eb' : '#3b82f6')
                : 'transparent',
              color: currentConversation?.id === conversation.id
                ? '#ffffff'
                : (settings.theme === 'dark' ? '#ffffff' : '#000000'),
              borderRadius: '6px',
              cursor: 'pointer',
              transition: 'background-color 0.2s'
            }}
          >
            <div style={{
              fontSize: '14px',
              fontWeight: '500',
              marginBottom: '4px',
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              whiteSpace: 'nowrap'
            }}>
              {conversation.title}
            </div>
            <div style={{
              fontSize: '11px',
              opacity: 0.7,
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              whiteSpace: 'nowrap'
            }}>
              {conversation.messages.length > 0
                ? conversation.messages[conversation.messages.length - 1].content.slice(0, 50)
                : 'Empty conversation'}
            </div>
            <button
              onClick={(e) => handleDeleteConversation(e, conversation.id)}
              style={{
                position: 'absolute',
                right: '12px',
                top: '50%',
                transform: 'translateY(-50%)',
                backgroundColor: 'transparent',
                color: 'inherit',
                border: 'none',
                cursor: 'pointer',
                fontSize: '14px',
                opacity: 0,
                transition: 'opacity 0.2s'
              }}
              onMouseEnter={(e) => (e.currentTarget.style.opacity = '1')}
              onMouseLeave={(e) => (e.currentTarget.style.opacity = '0')}
            >
              🗑️
            </button>
          </div>
        ))
      )}
    </div>
  )
}
