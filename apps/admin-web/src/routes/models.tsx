import { createFileRoute } from '@tanstack/react-router'

export const Route = createFileRoute('/models')({
  component: Models,
})

function Models() {
  return (
    <div style={{ padding: '16px', gap: '16px' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <h1 style={{ fontSize: '24px', fontWeight: 'bold', margin: 0 }}>Model Management</h1>
        <button style={{ padding: '8px 16px', borderRadius: '4px', border: '1px solid #e0e0e0', backgroundColor: 'white', cursor: 'pointer' }}>
          Add Model
        </button>
      </div>

      <div style={{ display: 'flex', gap: '16px', flexWrap: 'wrap' }}>
        {[
          { name: 'GPT-4', provider: 'OpenAI', enabled: true, rateLimit: '100/min', cost: '$0.03/1K tokens' },
          { name: 'GPT-3.5-Turbo', provider: 'OpenAI', enabled: true, rateLimit: '500/min', cost: '$0.002/1K tokens' },
          { name: 'Claude-3-Opus', provider: 'Anthropic', enabled: true, rateLimit: '50/min', cost: '$0.015/1K tokens' },
          { name: 'Claude-3-Sonnet', provider: 'Anthropic', enabled: true, rateLimit: '200/min', cost: '$0.003/1K tokens' },
          { name: 'Llama-2-70B', provider: 'Meta', enabled: false, rateLimit: 'N/A', cost: 'Self-hosted' },
          { name: 'Mistral-Large', provider: 'Mistral AI', enabled: true, rateLimit: '100/min', cost: '$0.004/1K tokens' },
        ].map((model, i) => (
          <div key={i} style={{ flex: 1, minWidth: 350, padding: '16px', backgroundColor: 'white', borderRadius: '8px', border: '1px solid #e0e0e0' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '8px' }}>
              <h3 style={{ fontSize: '16px', fontWeight: 'bold', margin: 0 }}>{model.name}</h3>
              <input type="checkbox" checked={model.enabled} readOnly style={{ cursor: 'pointer' }} />
            </div>

            <p style={{ fontSize: '12px', color: '#757575', margin: '0 0 8px 0' }}>{model.provider}</p>

            <div style={{ display: 'flex', gap: '16px', marginTop: '8px' }}>
              <div style={{ flex: 1 }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
                  <span>⚡</span>
                  <span style={{ fontSize: '12px' }}>Rate Limit</span>
                </div>
                <p style={{ fontSize: '14px', fontWeight: 'bold', margin: '4px 0 0 0' }}>{model.rateLimit}</p>
              </div>

              <div style={{ flex: 1 }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
                  <span>💰</span>
                  <span style={{ fontSize: '12px' }}>Cost</span>
                </div>
                <p style={{ fontSize: '14px', fontWeight: 'bold', margin: '4px 0 0 0' }}>{model.cost}</p>
              </div>
            </div>

            <div style={{ display: 'flex', gap: '8px', marginTop: '16px' }}>
              <button style={{ flex: 1, padding: '8px', borderRadius: '4px', border: '1px solid #e0e0e0', backgroundColor: 'white', cursor: 'pointer' }}>
                Configure
              </button>
              <button style={{ flex: 1, padding: '8px', borderRadius: '4px', border: '1px solid #e0e0e0', backgroundColor: 'white', cursor: 'pointer' }}>
                View Stats
              </button>
            </div>
          </div>
        ))}
      </div>

      <div style={{ padding: '16px', backgroundColor: 'white', borderRadius: '8px', border: '1px solid #e0e0e0' }}>
        <h3 style={{ fontSize: '16px', fontWeight: 'bold', margin: '0 0 8px 0' }}>Global Rate Limit Settings</h3>
        <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <label style={{ width: 200, fontSize: '14px' }}>Default Rate Limit:</label>
            <input type="text" defaultValue="100/min" style={{ flex: 1, padding: '8px', borderRadius: '4px', border: '1px solid #e0e0e0' }} />
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <label style={{ width: 200, fontSize: '14px' }}>Burst Allowance:</label>
            <input type="text" defaultValue="10" style={{ flex: 1, padding: '8px', borderRadius: '4px', border: '1px solid #e0e0e0' }} />
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <label style={{ width: 200, fontSize: '14px' }}>Cost Tracking:</label>
            <input type="checkbox" checked={true} readOnly style={{ cursor: 'pointer' }} />
          </div>
        </div>
        <button style={{ marginTop: '16px', padding: '8px 16px', borderRadius: '4px', border: '1px solid #e0e0e0', backgroundColor: 'white', cursor: 'pointer' }}>
          Save Settings
        </button>
      </div>
    </div>
  )
}
