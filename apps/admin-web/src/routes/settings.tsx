import { createFileRoute } from '@tanstack/react-router'

export const Route = createFileRoute('/settings')({
  component: SettingsPage,
})

function SettingsPage() {
  return (
    <div style={{ padding: '16px', gap: '16px' }}>
      <h1 style={{ fontSize: '24px', fontWeight: 'bold', margin: 0 }}>Settings</h1>

      <div style={{ padding: '16px', backgroundColor: 'white', borderRadius: '8px', border: '1px solid #e0e0e0' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '16px' }}>
          <span>⚙️</span>
          <h3 style={{ fontSize: '16px', fontWeight: 'bold', margin: 0 }}>Global Configuration</h3>
        </div>
        <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <label style={{ width: 250, fontSize: '14px' }}>API Base URL:</label>
            <input type="text" defaultValue="https://api.synaxis.dev" style={{ flex: 1, padding: '8px', borderRadius: '4px', border: '1px solid #e0e0e0' }} />
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <label style={{ width: 250, fontSize: '14px' }}>Default Model:</label>
            <input type="text" defaultValue="gpt-4" style={{ flex: 1, padding: '8px', borderRadius: '4px', border: '1px solid #e0e0e0' }} />
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <label style={{ width: 250, fontSize: '14px' }}>Max Tokens:</label>
            <input type="text" defaultValue="4096" style={{ flex: 1, padding: '8px', borderRadius: '4px', border: '1px solid #e0e0e0' }} />
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <label style={{ width: 250, fontSize: '14px' }}>Temperature:</label>
            <input type="text" defaultValue="0.7" style={{ flex: 1, padding: '8px', borderRadius: '4px', border: '1px solid #e0e0e0' }} />
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <label style={{ width: 250, fontSize: '14px' }}>Enable Streaming:</label>
            <input type="checkbox" checked={true} readOnly style={{ cursor: 'pointer' }} />
          </div>
        </div>
        <button style={{ marginTop: '16px', padding: '8px 16px', borderRadius: '4px', border: '1px solid #e0e0e0', backgroundColor: 'white', cursor: 'pointer' }}>
          Save Configuration
        </button>
      </div>

      <div style={{ padding: '16px', backgroundColor: 'white', borderRadius: '8px', border: '1px solid #e0e0e0' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '16px' }}>
          <span>🔀</span>
          <h3 style={{ fontSize: '16px', fontWeight: 'bold', margin: 0 }}>Feature Flags</h3>
        </div>
        <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
          {['Multi-tenancy', 'Rate Limiting', 'Analytics', 'Audit Logging', 'Webhooks', 'Custom Models'].map((flag, i) => (
            <div key={i} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '8px 0', borderBottom: '1px solid #e0e0e0' }}>
              <span>{flag}</span>
              <input type="checkbox" checked={i < 4} readOnly style={{ cursor: 'pointer' }} />
            </div>
          ))}
        </div>
        <button style={{ marginTop: '16px', padding: '8px 16px', borderRadius: '4px', border: '1px solid #e0e0e0', backgroundColor: 'white', cursor: 'pointer' }}>
          Save Feature Flags
        </button>
      </div>

      <div style={{ padding: '16px', backgroundColor: 'white', borderRadius: '8px', border: '1px solid #e0e0e0' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '16px' }}>
          <span>🛡️</span>
          <h3 style={{ fontSize: '16px', fontWeight: 'bold', margin: 0 }}>Maintenance Mode</h3>
        </div>
        <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <span>Enable Maintenance Mode</span>
            <input type="checkbox" checked={false} readOnly style={{ cursor: 'pointer' }} />
          </div>
          <div style={{ display: 'flex', alignItems: 'flex-start', gap: '8px', marginTop: '8px' }}>
            <label style={{ width: 250, fontSize: '14px' }}>Maintenance Message:</label>
            <textarea
              defaultValue="System is under maintenance. Please try again later."
              style={{ flex: 1, padding: '8px', borderRadius: '4px', border: '1px solid #e0e0e0', minHeight: 80 }}
            />
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <label style={{ width: 250, fontSize: '14px' }}>Allowed IPs (comma-separated):</label>
            <input type="text" defaultValue="127.0.0.1, 10.0.0.0/8" style={{ flex: 1, padding: '8px', borderRadius: '4px', border: '1px solid #e0e0e0' }} />
          </div>
        </div>
        <button style={{ marginTop: '16px', padding: '8px 16px', borderRadius: '4px', border: '1px solid #e0e0e0', backgroundColor: 'white', cursor: 'pointer' }}>
          Save Maintenance Settings
        </button>
      </div>

      <div style={{ padding: '16px', backgroundColor: 'white', borderRadius: '8px', border: '1px solid #e0e0e0' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '16px' }}>
          <span>📢</span>
          <h3 style={{ fontSize: '16px', fontWeight: 'bold', margin: 0 }}>Announcements</h3>
        </div>
        <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
          <div style={{ display: 'flex', alignItems: 'flex-start', gap: '8px' }}>
            <label style={{ width: 250, fontSize: '14px' }}>Active Announcement:</label>
            <textarea
              defaultValue="Welcome to Synaxis! We're excited to have you here."
              style={{ flex: 1, padding: '8px', borderRadius: '4px', border: '1px solid #e0e0e0', minHeight: 80 }}
            />
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <label style={{ width: 250, fontSize: '14px' }}>Announcement Type:</label>
            <input type="text" defaultValue="info" style={{ flex: 1, padding: '8px', borderRadius: '4px', border: '1px solid #e0e0e0' }} />
          </div>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <span>Show Announcement</span>
            <input type="checkbox" checked={true} readOnly style={{ cursor: 'pointer' }} />
          </div>
        </div>
        <button style={{ marginTop: '16px', padding: '8px 16px', borderRadius: '4px', border: '1px solid #e0e0e0', backgroundColor: 'white', cursor: 'pointer' }}>
          Save Announcement
        </button>
      </div>

      <div style={{ padding: '16px', backgroundColor: 'white', borderRadius: '8px', border: '1px solid #e0e0e0' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '16px' }}>
          <span>🔔</span>
          <h3 style={{ fontSize: '16px', fontWeight: 'bold', margin: 0 }}>Alert Settings</h3>
        </div>
        <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
          {['Email Alerts', 'Slack Notifications'].map((alert, i) => (
            <div key={i} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '8px 0', borderBottom: '1px solid #e0e0e0' }}>
              <span>{alert}</span>
              <input type="checkbox" checked={true} readOnly style={{ cursor: 'pointer' }} />
            </div>
          ))}
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '8px 0', borderBottom: '1px solid #e0e0e0' }}>
            <span>Error Rate Threshold (%)</span>
            <input type="text" defaultValue="5" style={{ width: 100, padding: '8px', borderRadius: '4px', border: '1px solid #e0e0e0' }} />
          </div>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '8px 0', borderBottom: '1px solid #e0e0e0' }}>
            <span>Latency Threshold (ms)</span>
            <input type="text" defaultValue="1000" style={{ width: 100, padding: '8px', borderRadius: '4px', border: '1px solid #e0e0e0' }} />
          </div>
        </div>
        <button style={{ marginTop: '16px', padding: '8px 16px', borderRadius: '4px', border: '1px solid #e0e0e0', backgroundColor: 'white', cursor: 'pointer' }}>
          Save Alert Settings
        </button>
      </div>
    </div>
  )
}
