import { useState } from 'react'
import { useStore } from '../App'

export function Settings() {
  const { settings, setSettings, setCurrentPage } = useStore()
  const [localSettings, setLocalSettings] = useState(settings)

  const handleChange = (key: keyof typeof settings, value: any) => {
    setLocalSettings({ ...localSettings, [key]: value })
  }

  const handleSave = () => {
    setSettings(localSettings)
    window.electronAPI.showNotification({
      title: 'Settings Saved',
      body: 'Your settings have been updated'
    })
  }

  const handleExportConversations = async () => {
    const conversations = await window.electronAPI.getConversations()
    const dataStr = JSON.stringify(conversations, null, 2)
    const dataBlob = new Blob([dataStr], { type: 'application/json' })
    const url = URL.createObjectURL(dataBlob)
    const link = document.createElement('a')
    link.href = url
    link.download = `synaxis-conversations-${new Date().toISOString().split('T')[0]}.json`
    link.click()
    URL.revokeObjectURL(url)
  }

  const availableModels = [
    { id: 'gpt-4', name: 'GPT-4', description: 'Most capable model', maxTokens: 8192 },
    { id: 'gpt-4-turbo', name: 'GPT-4 Turbo', description: 'Faster and cheaper', maxTokens: 128000 },
    { id: 'gpt-3.5-turbo', name: 'GPT-3.5 Turbo', description: 'Fast and efficient', maxTokens: 4096 }
  ]

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
        backgroundColor: settings.theme === 'dark' ? '#0d0d0d' : '#f5f5f5',
        padding: '16px'
      }}>
        <button
          onClick={() => setCurrentPage('chat')}
          style={{
            width: '100%',
            padding: '10px 16px',
            marginBottom: '12px',
            backgroundColor: 'transparent',
            color: settings.theme === 'dark' ? '#ffffff' : '#000000',
            border: `1px solid ${settings.theme === 'dark' ? '#333' : '#e0e0e0'}`,
            borderRadius: '6px',
            cursor: 'pointer',
            fontSize: '14px'
          }}
        >
          ← Back to Chat
        </button>

        <div style={{
          fontSize: '18px',
          fontWeight: '600',
          marginBottom: '20px',
          color: settings.theme === 'dark' ? '#ffffff' : '#000000'
        }}>
          Settings
        </div>

        <div style={{
          display: 'flex',
          flexDirection: 'column',
          gap: '8px'
        }}>
          <button
            onClick={() => document.getElementById('api-section')?.scrollIntoView({ behavior: 'smooth' })}
            style={{
              padding: '10px 16px',
              backgroundColor: 'transparent',
              color: settings.theme === 'dark' ? '#ffffff' : '#000000',
              border: 'none',
              borderRadius: '6px',
              cursor: 'pointer',
              fontSize: '14px',
              textAlign: 'left'
            }}
          >
            🔑 API Configuration
          </button>
          <button
            onClick={() => document.getElementById('model-section')?.scrollIntoView({ behavior: 'smooth' })}
            style={{
              padding: '10px 16px',
              backgroundColor: 'transparent',
              color: settings.theme === 'dark' ? '#ffffff' : '#000000',
              border: 'none',
              borderRadius: '6px',
              cursor: 'pointer',
              fontSize: '14px',
              textAlign: 'left'
            }}
          >
            🤖 Model Preferences
          </button>
          <button
            onClick={() => document.getElementById('appearance-section')?.scrollIntoView({ behavior: 'smooth' })}
            style={{
              padding: '10px 16px',
              backgroundColor: 'transparent',
              color: settings.theme === 'dark' ? '#ffffff' : '#000000',
              border: 'none',
              borderRadius: '6px',
              cursor: 'pointer',
              fontSize: '14px',
              textAlign: 'left'
            }}
          >
            🎨 Appearance
          </button>
          <button
            onClick={() => document.getElementById('data-section')?.scrollIntoView({ behavior: 'smooth' })}
            style={{
              padding: '10px 16px',
              backgroundColor: 'transparent',
              color: settings.theme === 'dark' ? '#ffffff' : '#000000',
              border: 'none',
              borderRadius: '6px',
              cursor: 'pointer',
              fontSize: '14px',
              textAlign: 'left'
            }}
          >
            📦 Data Management
          </button>
        </div>
      </div>

      {/* Settings Content */}
      <div style={{
        flex: 1,
        overflowY: 'auto',
        padding: '32px'
      }}>
        <div style={{ maxWidth: '800px', margin: '0 auto' }}>
          <h1 style={{
            fontSize: '28px',
            fontWeight: '600',
            marginBottom: '32px',
            color: settings.theme === 'dark' ? '#ffffff' : '#000000'
          }}>
            Settings
          </h1>

          {/* API Configuration */}
          <section id="api-section" style={{ marginBottom: '32px' }}>
            <h2 style={{
              fontSize: '20px',
              fontWeight: '600',
              marginBottom: '16px',
              color: settings.theme === 'dark' ? '#ffffff' : '#000000'
            }}>
              🔑 API Configuration
            </h2>
            <div style={{
              padding: '20px',
              backgroundColor: settings.theme === 'dark' ? '#0d0d0d' : '#f5f5f5',
              borderRadius: '8px',
              border: `1px solid ${settings.theme === 'dark' ? '#333' : '#e0e0e0'}`
            }}>
              <div style={{ marginBottom: '16px' }}>
                <label style={{
                  display: 'block',
                  marginBottom: '8px',
                  fontSize: '14px',
                  fontWeight: '500',
                  color: settings.theme === 'dark' ? '#ffffff' : '#000000'
                }}>
                  API Key
                </label>
                <input
                  type="password"
                  value={localSettings.apiKey}
                  onChange={(e) => handleChange('apiKey', e.target.value)}
                  placeholder="Enter your API key"
                  style={{
                    width: '100%',
                    padding: '10px 12px',
                    backgroundColor: settings.theme === 'dark' ? '#1a1a1a' : '#ffffff',
                    color: settings.theme === 'dark' ? '#ffffff' : '#000000',
                    border: `1px solid ${settings.theme === 'dark' ? '#333' : '#e0e0e0'}`,
                    borderRadius: '6px',
                    fontSize: '14px',
                    outline: 'none'
                  }}
                />
                <div style={{
                  marginTop: '6px',
                  fontSize: '12px',
                  color: settings.theme === 'dark' ? '#666' : '#999'
                }}>
                  Your API key is stored locally and never sent to our servers
                </div>
              </div>
            </div>
          </section>

          {/* Model Preferences */}
          <section id="model-section" style={{ marginBottom: '32px' }}>
            <h2 style={{
              fontSize: '20px',
              fontWeight: '600',
              marginBottom: '16px',
              color: settings.theme === 'dark' ? '#ffffff' : '#000000'
            }}>
              🤖 Model Preferences
            </h2>
            <div style={{
              padding: '20px',
              backgroundColor: settings.theme === 'dark' ? '#0d0d0d' : '#f5f5f5',
              borderRadius: '8px',
              border: `1px solid ${settings.theme === 'dark' ? '#333' : '#e0e0e0'}`
            }}>
              <div style={{ marginBottom: '16px' }}>
                <label style={{
                  display: 'block',
                  marginBottom: '8px',
                  fontSize: '14px',
                  fontWeight: '500',
                  color: settings.theme === 'dark' ? '#ffffff' : '#000000'
                }}>
                  Default Model
                </label>
                <select
                  value={localSettings.defaultModel}
                  onChange={(e) => handleChange('defaultModel', e.target.value)}
                  style={{
                    width: '100%',
                    padding: '10px 12px',
                    backgroundColor: settings.theme === 'dark' ? '#1a1a1a' : '#ffffff',
                    color: settings.theme === 'dark' ? '#ffffff' : '#000000',
                    border: `1px solid ${settings.theme === 'dark' ? '#333' : '#e0e0e0'}`,
                    borderRadius: '6px',
                    fontSize: '14px',
                    outline: 'none'
                  }}
                >
                  {availableModels.map(model => (
                    <option key={model.id} value={model.id}>
                      {model.name} - {model.description} (Max: {model.maxTokens} tokens)
                    </option>
                  ))}
                </select>
              </div>

              <div style={{ marginBottom: '16px' }}>
                <label style={{
                  display: 'block',
                  marginBottom: '8px',
                  fontSize: '14px',
                  fontWeight: '500',
                  color: settings.theme === 'dark' ? '#ffffff' : '#000000'
                }}>
                  Temperature: {localSettings.temperature}
                </label>
                <input
                  type="range"
                  min="0"
                  max="2"
                  step="0.1"
                  value={localSettings.temperature}
                  onChange={(e) => handleChange('temperature', parseFloat(e.target.value))}
                  style={{ width: '100%' }}
                />
                <div style={{
                  marginTop: '6px',
                  fontSize: '12px',
                  color: settings.theme === 'dark' ? '#666' : '#999'
                }}>
                  Lower values make responses more focused, higher values more creative
                </div>
              </div>

              <div style={{ marginBottom: '16px' }}>
                <label style={{
                  display: 'block',
                  marginBottom: '8px',
                  fontSize: '14px',
                  fontWeight: '500',
                  color: settings.theme === 'dark' ? '#ffffff' : '#000000'
                }}>
                  Top P: {localSettings.topP}
                </label>
                <input
                  type="range"
                  min="0"
                  max="1"
                  step="0.05"
                  value={localSettings.topP}
                  onChange={(e) => handleChange('topP', parseFloat(e.target.value))}
                  style={{ width: '100%' }}
                />
                <div style={{
                  marginTop: '6px',
                  fontSize: '12px',
                  color: settings.theme === 'dark' ? '#666' : '#999'
                }}>
                  Controls diversity via nucleus sampling
                </div>
              </div>

              <div>
                <label style={{
                  display: 'block',
                  marginBottom: '8px',
                  fontSize: '14px',
                  fontWeight: '500',
                  color: settings.theme === 'dark' ? '#ffffff' : '#000000'
                }}>
                  Max Tokens: {localSettings.maxTokens}
                </label>
                <input
                  type="number"
                  min="1"
                  max="128000"
                  value={localSettings.maxTokens}
                  onChange={(e) => handleChange('maxTokens', parseInt(e.target.value))}
                  style={{
                    width: '100%',
                    padding: '10px 12px',
                    backgroundColor: settings.theme === 'dark' ? '#1a1a1a' : '#ffffff',
                    color: settings.theme === 'dark' ? '#ffffff' : '#000000',
                    border: `1px solid ${settings.theme === 'dark' ? '#333' : '#e0e0e0'}`,
                    borderRadius: '6px',
                    fontSize: '14px',
                    outline: 'none'
                  }}
                />
              </div>
            </div>
          </section>

          {/* Appearance */}
          <section id="appearance-section" style={{ marginBottom: '32px' }}>
            <h2 style={{
              fontSize: '20px',
              fontWeight: '600',
              marginBottom: '16px',
              color: settings.theme === 'dark' ? '#ffffff' : '#000000'
            }}>
              🎨 Appearance
            </h2>
            <div style={{
              padding: '20px',
              backgroundColor: settings.theme === 'dark' ? '#0d0d0d' : '#f5f5f5',
              borderRadius: '8px',
              border: `1px solid ${settings.theme === 'dark' ? '#333' : '#e0e0e0'}`
            }}>
              <div>
                <label style={{
                  display: 'block',
                  marginBottom: '8px',
                  fontSize: '14px',
                  fontWeight: '500',
                  color: settings.theme === 'dark' ? '#ffffff' : '#000000'
                }}>
                  Theme
                </label>
                <div style={{ display: 'flex', gap: '12px' }}>
                  <button
                    onClick={() => handleChange('theme', 'light')}
                    style={{
                      flex: 1,
                      padding: '12px',
                      backgroundColor: localSettings.theme === 'light' ? '#3b82f6' : 'transparent',
                      color: localSettings.theme === 'light' ? '#ffffff' : (settings.theme === 'dark' ? '#ffffff' : '#000000'),
                      border: `1px solid ${settings.theme === 'dark' ? '#333' : '#e0e0e0'}`,
                      borderRadius: '6px',
                      cursor: 'pointer',
                      fontSize: '14px'
                    }}
                  >
                    ☀️ Light
                  </button>
                  <button
                    onClick={() => handleChange('theme', 'dark')}
                    style={{
                      flex: 1,
                      padding: '12px',
                      backgroundColor: localSettings.theme === 'dark' ? '#3b82f6' : 'transparent',
                      color: localSettings.theme === 'dark' ? '#ffffff' : (settings.theme === 'dark' ? '#ffffff' : '#000000'),
                      border: `1px solid ${settings.theme === 'dark' ? '#333' : '#e0e0e0'}`,
                      borderRadius: '6px',
                      cursor: 'pointer',
                      fontSize: '14px'
                    }}
                  >
                    🌙 Dark
                  </button>
                </div>
              </div>
            </div>
          </section>

          {/* Data Management */}
          <section id="data-section" style={{ marginBottom: '32px' }}>
            <h2 style={{
              fontSize: '20px',
              fontWeight: '600',
              marginBottom: '16px',
              color: settings.theme === 'dark' ? '#ffffff' : '#000000'
            }}>
              📦 Data Management
            </h2>
            <div style={{
              padding: '20px',
              backgroundColor: settings.theme === 'dark' ? '#0d0d0d' : '#f5f5f5',
              borderRadius: '8px',
              border: `1px solid ${settings.theme === 'dark' ? '#333' : '#e0e0e0'}`
            }}>
              <button
                onClick={handleExportConversations}
                style={{
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
                Export Conversations
              </button>
              <div style={{
                marginTop: '8px',
                fontSize: '12px',
                color: settings.theme === 'dark' ? '#666' : '#999'
              }}>
                Download all your conversations as a JSON file
              </div>
            </div>
          </section>

          {/* Save Button */}
          <div style={{
            display: 'flex',
            justifyContent: 'flex-end',
            gap: '12px'
          }}>
            <button
              onClick={() => setLocalSettings(settings)}
              style={{
                padding: '10px 20px',
                backgroundColor: 'transparent',
                color: settings.theme === 'dark' ? '#ffffff' : '#000000',
                border: `1px solid ${settings.theme === 'dark' ? '#333' : '#e0e0e0'}`,
                borderRadius: '6px',
                cursor: 'pointer',
                fontSize: '14px'
              }}
            >
              Cancel
            </button>
            <button
              onClick={handleSave}
              style={{
                padding: '10px 20px',
                backgroundColor: settings.theme === 'dark' ? '#2563eb' : '#3b82f6',
                color: 'white',
                border: 'none',
                borderRadius: '6px',
                cursor: 'pointer',
                fontSize: '14px',
                fontWeight: '500'
              }}
            >
              Save Settings
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}
