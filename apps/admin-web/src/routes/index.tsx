import { createFileRoute } from '@tanstack/react-router'
import { RequestsChart } from '../components/charts/RequestsChart'

export const Route = createFileRoute('/')({
  component: Dashboard,
})

function Dashboard() {
  return (
    <div style={{ padding: '16px', gap: '16px' }}>
      <h1 style={{ fontSize: '24px', fontWeight: 'bold', margin: 0 }}>
        Dashboard
      </h1>

      <div style={{ display: 'flex', gap: '16px', flexWrap: 'wrap' }}>
        <div style={{ flex: 1, minWidth: 250, padding: '16px', backgroundColor: 'white', borderRadius: '8px', border: '1px solid #e0e0e0' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <span>👥</span>
            <h3 style={{ fontSize: '16px', fontWeight: 'bold', margin: 0 }}>Active Users</h3>
          </div>
          <p style={{ fontSize: '32px', fontWeight: 'bold', margin: '8px 0 0 0' }}>1,234</p>
          <p style={{ fontSize: '12px', color: '#4caf50', margin: '4px 0 0 0' }}>+12% from last week</p>
        </div>

        <div style={{ flex: 1, minWidth: 250, padding: '16px', backgroundColor: 'white', borderRadius: '8px', border: '1px solid #e0e0e0' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <span>📊</span>
            <h3 style={{ fontSize: '16px', fontWeight: 'bold', margin: 0 }}>Requests/Day</h3>
          </div>
          <p style={{ fontSize: '32px', fontWeight: 'bold', margin: '8px 0 0 0' }}>45,678</p>
          <p style={{ fontSize: '12px', color: '#4caf50', margin: '4px 0 0 0' }}>+8% from last week</p>
        </div>

        <div style={{ flex: 1, minWidth: 250, padding: '16px', backgroundColor: 'white', borderRadius: '8px', border: '1px solid #e0e0e0' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <span>⚡</span>
            <h3 style={{ fontSize: '16px', fontWeight: 'bold', margin: 0 }}>Avg Latency</h3>
          </div>
          <p style={{ fontSize: '32px', fontWeight: 'bold', margin: '8px 0 0 0' }}>245ms</p>
          <p style={{ fontSize: '12px', color: '#4caf50', margin: '4px 0 0 0' }}>-15% from last week</p>
        </div>

        <div style={{ flex: 1, minWidth: 250, padding: '16px', backgroundColor: 'white', borderRadius: '8px', border: '1px solid #e0e0e0' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <span>⚠️</span>
            <h3 style={{ fontSize: '16px', fontWeight: 'bold', margin: 0 }}>Alerts</h3>
          </div>
          <p style={{ fontSize: '32px', fontWeight: 'bold', margin: '8px 0 0 0' }}>3</p>
          <p style={{ fontSize: '12px', color: '#f44336', margin: '4px 0 0 0' }}>2 critical, 1 warning</p>
        </div>
      </div>

      <div style={{ padding: '16px', backgroundColor: 'white', borderRadius: '8px', border: '1px solid #e0e0e0' }}>
        <h3 style={{ fontSize: '16px', fontWeight: 'bold', margin: '0 0 8px 0' }}>Request Volume (Last 7 Days)</h3>
        <RequestsChart />
      </div>

      <div style={{ padding: '16px', backgroundColor: 'white', borderRadius: '8px', border: '1px solid #e0e0e0' }}>
        <h3 style={{ fontSize: '16px', fontWeight: 'bold', margin: '0 0 8px 0' }}>System Health</h3>
        <div style={{ display: 'flex', gap: '16px' }}>
          <div style={{ flex: 1 }}>
            <p style={{ fontSize: '12px', margin: '0 0 4px 0' }}>API Gateway</p>
            <p style={{ fontSize: '14px', color: '#4caf50', fontWeight: 'bold', margin: 0 }}>Healthy</p>
          </div>
          <div style={{ flex: 1 }}>
            <p style={{ fontSize: '12px', margin: '0 0 4px 0' }}>Database</p>
            <p style={{ fontSize: '14px', color: '#4caf50', fontWeight: 'bold', margin: 0 }}>Healthy</p>
          </div>
          <div style={{ flex: 1 }}>
            <p style={{ fontSize: '12px', margin: '0 0 4px 0' }}>Cache</p>
            <p style={{ fontSize: '14px', color: '#4caf50', fontWeight: 'bold', margin: 0 }}>Healthy</p>
          </div>
          <div style={{ flex: 1 }}>
            <p style={{ fontSize: '12px', margin: '0 0 4px 0' }}>Queue</p>
            <p style={{ fontSize: '14px', color: '#ff9800', fontWeight: 'bold', margin: 0 }}>Degraded</p>
          </div>
        </div>
      </div>
    </div>
  )
}
