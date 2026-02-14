import { useStore } from '../App'

interface ModelSelectorProps {
  className?: string
}

export function ModelSelector({ className }: ModelSelectorProps) {
  const { settings, setSettings } = useStore()

  const availableModels = [
    { id: 'gpt-4', name: 'GPT-4', description: 'Most capable', maxTokens: 8192 },
    { id: 'gpt-4-turbo', name: 'GPT-4 Turbo', description: 'Faster', maxTokens: 128000 },
    { id: 'gpt-3.5-turbo', name: 'GPT-3.5 Turbo', description: 'Fast', maxTokens: 4096 }
  ]

  return (
    <div
      className={className}
      style={{
        display: 'flex',
        alignItems: 'center',
        gap: '12px'
      }}
    >
      <div style={{
        padding: '8px 12px',
        backgroundColor: settings.theme === 'dark' ? '#0d0d0d' : '#f5f5f5',
        borderRadius: '6px',
        border: `1px solid ${settings.theme === 'dark' ? '#333' : '#e0e0e0'}`
      }}>
        <select
          value={settings.defaultModel}
          onChange={(e) => setSettings({ ...settings, defaultModel: e.target.value })}
          style={{
            backgroundColor: 'transparent',
            color: settings.theme === 'dark' ? '#ffffff' : '#000000',
            border: 'none',
            fontSize: '14px',
            fontWeight: '500',
            outline: 'none',
            cursor: 'pointer'
          }}
        >
          {availableModels.map(model => (
            <option key={model.id} value={model.id}>
              {model.name}
            </option>
          ))}
        </select>
      </div>

      <div style={{
        display: 'flex',
        gap: '16px',
        fontSize: '12px',
        color: settings.theme === 'dark' ? '#888' : '#666'
      }}>
        <span>🌡️ {settings.temperature}</span>
        <span>🎯 {settings.topP}</span>
        <span>📊 {settings.maxTokens} tokens</span>
      </div>
    </div>
  )
}
