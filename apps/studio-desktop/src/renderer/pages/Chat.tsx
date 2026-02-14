import React, { useState, useRef, useEffect } from 'react'
import { useStore } from '../App'
import { ModelSelector } from '../components/ModelSelector'
import { ConversationList } from '../components/ConversationList'
import { MessageBubble } from '../components/MessageBubble'

export function Chat() {
  const {
    currentConversation,
    addMessage,
    createNewConversation,
    settings,
    setCurrentPage
  } = useStore()

  const [input, setInput] = useState('')
  const [isStreaming, setIsStreaming] = useState(false)
  const [streamedContent, setStreamedContent] = useState('')
  const messagesEndRef = useRef<HTMLDivElement>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }

  useEffect(() => {
    scrollToBottom()
  }, [currentConversation?.messages, streamedContent])

  const handleSend = async () => {
    if (!input.trim() || isStreaming) return

    const userMessage = {
      id: Date.now().toString(),
      role: 'user' as const,
      content: input,
      timestamp: new Date()
    }

    addMessage(userMessage)
    setInput('')
    setIsStreaming(true)
    setStreamedContent('')

    // Simulate streaming response
    await simulateStreamingResponse(input)
  }

  const simulateStreamingResponse = async (userInput: string) => {
    const response = `This is a simulated response to: "${userInput}".\n\nIn a real implementation, this would connect to an AI API and stream the response token by token.\n\nThe streaming feature allows for a more natural conversation experience, showing the AI's thinking process in real-time.`

    let currentContent = ''
    for (let i = 0; i < response.length; i++) {
      currentContent += response[i]
      setStreamedContent(currentContent)
      await new Promise(resolve => setTimeout(resolve, 20))
    }

    const assistantMessage = {
      id: (Date.now() + 1).toString(),
      role: 'assistant' as const,
      content: response,
      timestamp: new Date()
    }

    addMessage(assistantMessage)
    setIsStreaming(false)
    setStreamedContent('')
  }

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      handleSend()
    }
  }

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files
    if (files && files.length > 0) {
      console.log('Selected files:', Array.from(files))
      // Handle file attachments
    }
  }

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault()
  }

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault()
    const files = e.dataTransfer.files
    if (files.length > 0) {
      console.log('Dropped files:', Array.from(files))
      // Handle file attachments
    }
  }

  const handleCopy = (content: string) => {
    navigator.clipboard.writeText(content)
    window.electronAPI.showNotification({
      title: 'Copied',
      body: 'Message copied to clipboard'
    })
  }

  const handleRegenerate = async () => {
    if (!currentConversation || currentConversation.messages.length === 0) return

    const lastMessage = currentConversation.messages[currentConversation.messages.length - 1]
    if (lastMessage.role === 'assistant') {
      // Remove last assistant message and regenerate
      const updatedMessages = currentConversation.messages.slice(0, -1)
      const updatedConversation = {
        ...currentConversation,
        messages: updatedMessages
      }
      useStore.getState().setCurrentConversation(updatedConversation)

      // Get the last user message
      const lastUserMessage = [...updatedMessages].reverse().find(m => m.role === 'user')
      if (lastUserMessage) {
        setIsStreaming(true)
        setStreamedContent('')
        await simulateStreamingResponse(lastUserMessage.content)
      }
    }
  }

  return (
    <div style={{
      display: 'flex',
      height: '100%',
      backgroundColor: settings.theme === 'dark' ? '#1a1a1a' : '#ffffff'
    }}>
      {/* Sidebar */}
      <div style={{
        width: '280px',
        borderRight: `1px solid ${settings.theme === 'dark' ? '#333' : '#e0e0e0'}`,
        display: 'flex',
        flexDirection: 'column',
        backgroundColor: settings.theme === 'dark' ? '#0d0d0d' : '#f5f5f5'
      }}>
        <div style={{
          padding: '16px',
          borderBottom: `1px solid ${settings.theme === 'dark' ? '#333' : '#e0e0e0'}`
        }}>
          <button
            onClick={createNewConversation}
            style={{
              width: '100%',
              padding: '10px 16px',
              backgroundColor: settings.theme === 'dark' ? '#2563eb' : '#3b82f6',
              color: 'white',
              border: 'none',
              borderRadius: '6px',
              cursor: 'pointer',
              fontSize: '14px',
              fontWeight: '500'
            }}
          >
            + New Conversation
          </button>
        </div>

        <ConversationList />

        <div style={{
          marginTop: 'auto',
          padding: '16px',
          borderTop: `1px solid ${settings.theme === 'dark' ? '#333' : '#e0e0e0'}`
        }}>
          <button
            onClick={() => setCurrentPage('settings')}
            style={{
              width: '100%',
              padding: '10px 16px',
              backgroundColor: 'transparent',
              color: settings.theme === 'dark' ? '#ffffff' : '#000000',
              border: `1px solid ${settings.theme === 'dark' ? '#333' : '#e0e0e0'}`,
              borderRadius: '6px',
              cursor: 'pointer',
              fontSize: '14px'
            }}
          >
            ⚙️ Settings
          </button>
        </div>
      </div>

      {/* Main Chat Area */}
      <div style={{
        flex: 1,
        display: 'flex',
        flexDirection: 'column'
      }}>
        {/* Model Selector Bar */}
        <div style={{
          padding: '12px 16px',
          borderBottom: `1px solid ${settings.theme === 'dark' ? '#333' : '#e0e0e0'}`,
          backgroundColor: settings.theme === 'dark' ? '#1a1a1a' : '#ffffff'
        }}>
          <ModelSelector />
        </div>

        {/* Messages */}
        <div
          style={{
            flex: 1,
            overflowY: 'auto',
            padding: '20px'
          }}
          onDragOver={handleDragOver}
          onDrop={handleDrop}
        >
          {!currentConversation || currentConversation.messages.length === 0 ? (
            <div style={{
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              justifyContent: 'center',
              height: '100%',
              color: settings.theme === 'dark' ? '#888' : '#666'
            }}>
              <div style={{ fontSize: '48px', marginBottom: '16px' }}>💬</div>
              <div style={{ fontSize: '18px', marginBottom: '8px' }}>Start a conversation</div>
              <div style={{ fontSize: '14px' }}>
                Type a message below or drag and drop files to attach
              </div>
            </div>
          ) : (
            <>
              {currentConversation.messages.map((message) => (
                <MessageBubble
                  key={message.id}
                  message={message}
                  theme={settings.theme}
                  onCopy={handleCopy}
                  onRegenerate={handleRegenerate}
                />
              ))}
              {isStreaming && streamedContent && (
                <MessageBubble
                  message={{
                    id: 'streaming',
                    role: 'assistant',
                    content: streamedContent,
                    timestamp: new Date()
                  }}
                  theme={settings.theme}
                  isStreaming={true}
                  onCopy={handleCopy}
                  onRegenerate={handleRegenerate}
                />
              )}
              <div ref={messagesEndRef} />
            </>
          )}
        </div>

        {/* Input Area */}
        <div style={{
          padding: '16px',
          borderTop: `1px solid ${settings.theme === 'dark' ? '#333' : '#e0e0e0'}`,
          backgroundColor: settings.theme === 'dark' ? '#1a1a1a' : '#ffffff'
        }}>
          <div style={{
            display: 'flex',
            gap: '8px',
            alignItems: 'flex-end'
          }}>
            <input
              ref={fileInputRef}
              type="file"
              multiple
              style={{ display: 'none' }}
              onChange={handleFileSelect}
            />
            <button
              onClick={() => fileInputRef.current?.click()}
              style={{
                padding: '12px',
                backgroundColor: 'transparent',
                color: settings.theme === 'dark' ? '#888' : '#666',
                border: `1px solid ${settings.theme === 'dark' ? '#333' : '#e0e0e0'}`,
                borderRadius: '8px',
                cursor: 'pointer',
                fontSize: '18px'
              }}
              title="Attach files"
            >
              📎
            </button>
            <textarea
              value={input}
              onChange={(e) => setInput(e.target.value)}
              onKeyDown={handleKeyDown}
              placeholder="Type your message... (Shift+Enter for new line)"
              disabled={isStreaming}
              style={{
                flex: 1,
                minHeight: '48px',
                maxHeight: '200px',
                padding: '12px 16px',
                backgroundColor: settings.theme === 'dark' ? '#0d0d0d' : '#f5f5f5',
                color: settings.theme === 'dark' ? '#ffffff' : '#000000',
                border: `1px solid ${settings.theme === 'dark' ? '#333' : '#e0e0e0'}`,
                borderRadius: '8px',
                resize: 'none',
                fontSize: '14px',
                fontFamily: 'inherit',
                outline: 'none'
              }}
              rows={1}
            />
            <button
              onClick={handleSend}
              disabled={!input.trim() || isStreaming}
              style={{
                padding: '12px 20px',
                backgroundColor: !input.trim() || isStreaming
                  ? (settings.theme === 'dark' ? '#333' : '#ccc')
                  : (settings.theme === 'dark' ? '#2563eb' : '#3b82f6'),
                color: 'white',
                border: 'none',
                borderRadius: '8px',
                cursor: !input.trim() || isStreaming ? 'not-allowed' : 'pointer',
                fontSize: '14px',
                fontWeight: '500'
              }}
            >
              {isStreaming ? '⏳' : 'Send'}
            </button>
          </div>
          <div style={{
            marginTop: '8px',
            fontSize: '12px',
            color: settings.theme === 'dark' ? '#666' : '#999'
          }}>
            Press Enter to send, Shift+Enter for new line • Drag & drop files to attach
          </div>
        </div>
      </div>
    </div>
  )
}
