import { createFileRoute } from '@tanstack/react-router'

export const Route = createFileRoute('/monitoring')({
  component: Monitoring,
})

function Monitoring() {
  return (
    <div style={{ padding: '16px', gap: '16px' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <h1 style={{ fontSize: '24px', fontWeight: 'bold', margin: 0 }}>Monitoring</h1>
        <button style={{ padding: '8px 16px', borderRadius: '4px', border: '1px solid #e0e0e0', backgroundColor: 'white', cursor: 'pointer' }}>
          Refresh
        </button>
      </div>

      <div style={{ display: 'flex', gap: '16px', flexWrap: 'wrap' }}>
        <div style={{ flex: 1, minWidth: 250, padding: '16px', backgroundColor: 'white', borderRadius: '8px', border: '1px solid #e0e0e0' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <span>📊</span>
            <h3 style={{ fontSize: '16px', fontWeight: 'bold', margin: 0 }}>Requests/sec</h3>
          </div>
          <p style={{ fontSize: '32px', fontWeight: 'bold', margin: '8px 0 0 0' }}>523</p>
          <p style={{ fontSize: '12px', color: '#4caf50', margin: '4px 0 0 0' }}>+5% from last hour</p>
        </div>

        <div style={{ flex: 1, minWidth: 250, padding: '16px', backgroundColor: 'white', borderRadius: '8px', border: '1px solid #e0e0e0' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <span>⏱️</span>
            <h3 style={{ fontSize: '16px', fontWeight: 'bold', margin: 0 }}>Avg Response Time</h3>
          </div>
          <p style={{ fontSize: '32px', fontWeight: 'bold', margin: '8px 0 0 0' }}>245ms</p>
          <p style={{ fontSize: '12px', color: '#4caf50', margin: '4px 0 0 0' }}>-10% from last hour</p>
        </div>

        <div style={{ flex: 1, minWidth: 250, padding: '16px', backgroundColor: 'white', borderRadius: '8px', border: '1px solid #e0e0e0' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <span>⚠️</span>
            <h3 style={{ fontSize: '16px', fontWeight: 'bold', margin: 0 }}>Error Rate</h3>
          </div>
          <p style={{ fontSize: '32px', fontWeight: 'bold', margin: '8px 0 0 0' }}>0.12%</p>
          <p style={{ fontSize: '12px', color: '#4caf50', margin: '4px 0 0 0' }}>-0.05% from last hour</p>
        </div>

        <div style={{ flex: 1, minWidth: 250, padding: '16px', backgroundColor: 'white', borderRadius: '8px', border: '1px solid #e0e0e0' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <span>📈</span>
            <h3 style={{ fontSize: '16px', fontWeight: 'bold', margin: 0 }}>Success Rate</h3>
          </div>
          <p style={{ fontSize: '32px', fontWeight: 'bold', margin: '8px 0 0 0' }}>99.88%</p>
          <p style={{ fontSize: '12px', color: '#4caf50', margin: '4px 0 0 0' }}>+0.05% from last hour</p>
        </div>
      </div>

      <div style={{ padding: '16px', backgroundColor: 'white', borderRadius: '8px', border: '1px solid #e0e0e0' }}>
        <h3 style={{ fontSize: '16px', fontWeight: 'bold', margin: '0 0 8px 0' }}>Live Request Logs</h3>
        <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', maxHeight: 400, overflow: 'hidden' }}>
          {[
            { time: '21:45:23', method: 'POST', path: '/api/v1/chat', status: 200, latency: '245ms' },
            { time: '21:45:22', method: 'GET', path: '/api/v1/models', status: 200, latency: '12ms' },
            { time: '21:45:21', method: 'POST', path: '/api/v1/chat', status: 200, latency: '312ms' },
            { time: '21:45:20', method: 'POST', path: '/api/v1/chat', status: 429, latency: '5ms' },
            { time: '21:45:19', method: 'GET', path: '/api/v1/health', status: 200, latency: '3ms' },
            { time: '21:45:18', method: 'POST', path: '/api/v1/chat', status: 500, latency: '1234ms' },
            { time: '21:45:17', method: 'POST', path: '/api/v1/chat', status: 200, latency: '289ms' },
          ].map((log, i) => (
            <div
              key={i}
              style={{
                display: 'flex',
                gap: '8px',
                padding: '8px',
                backgroundColor: log.status >= 500 ? '#ffebee' : log.status >= 400 ? '#fff3e0' : '#e8f5e9',
                borderRadius: '4px',
              }}
            >
              <span style={{ width: 80, fontSize: '12px' }}>{log.time}</span>
              <span style={{ width: 60, fontSize: '12px', fontWeight: 'bold' }}>{log.method}</span>
              <span style={{ flex: 1, fontSize: '12px' }}>{log.path}</span>
              <span
                style={{
                  width: 50,
                  fontSize: '12px',
                  color: log.status >= 500 ? '#f44336' : log.status >= 400 ? '#ff9800' : '#4caf50',
                  fontWeight: 'bold',
                }}
              >
                {log.status}
              </span>
              <span style={{ width: 60, fontSize: '12px', textAlign: 'right' }}>{log.latency}</span>
            </div>
          ))}
        </div>
      </div>

      <div style={{ display: 'flex', gap: '16px' }}>
        <div style={{ flex: 1, padding: '16px', backgroundColor: 'white', borderRadius: '8px', border: '1px solid #e0e0e0' }}>
          <h3 style={{ fontSize: '16px', fontWeight: 'bold', margin: '0 0 8px 0' }}>Recent Errors</h3>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
            {[
              { error: 'Rate limit exceeded', count: 23, time: 'Last hour' },
              { error: 'Model unavailable', count: 5, time: 'Last hour' },
              { error: 'Timeout', count: 2, time: 'Last hour' },
            ].map((err, i) => (
              <div key={i} style={{ display: 'flex', justifyContent: 'space-between', padding: '8px 0', borderBottom: '1px solid #e0e0e0' }}>
                <span>{err.error}</span>
                <div style={{ display: 'flex', gap: '16px' }}>
                  <span style={{ color: '#f44336' }}>{err.count} occurrences</span>
                  <span style={{ color: '#757575' }}>{err.time}</span>
                </div>
              </div>
            ))}
          </div>
        </div>

        <div style={{ flex: 1, padding: '16px', backgroundColor: 'white', borderRadius: '8px', border: '1px solid #e0e0e0' }}>
          <h3 style={{ fontSize: '16px', fontWeight: 'bold', margin: '0 0 8px 0' }}>Audit Logs</h3>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
            {[
              { action: 'User banned', user: 'admin@acme.com', target: 'user123', time: '2 min ago' },
              { action: 'Model disabled', user: 'admin@acme.com', target: 'Llama-2-70B', time: '5 min ago' },
              { action: 'Org created', user: 'admin@acme.com', target: 'New Corp', time: '10 min ago' },
              { action: 'Settings updated', user: 'admin@acme.com', target: 'Global config', time: '15 min ago' },
            ].map((log, i) => (
              <div key={i} style={{ display: 'flex', justifyContent: 'space-between', padding: '8px 0', borderBottom: '1px solid #e0e0e0' }}>
                <span>{log.action}</span>
                <div style={{ display: 'flex', gap: '16px' }}>
                  <span style={{ color: '#2196f3' }}>{log.target}</span>
                  <span style={{ color: '#757575' }}>{log.time}</span>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  )
}
