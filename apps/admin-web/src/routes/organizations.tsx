import { createFileRoute } from '@tanstack/react-router'

export const Route = createFileRoute('/organizations')({
  component: Organizations,
})

function Organizations() {
  return (
    <div style={{ padding: '16px', gap: '16px' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <h1 style={{ fontSize: '24px', fontWeight: 'bold', margin: 0 }}>Organization Management</h1>
        <button style={{ padding: '8px 16px', borderRadius: '4px', border: '1px solid #e0e0e0', backgroundColor: 'white', cursor: 'pointer' }}>
          New Organization
        </button>
      </div>

      <div style={{ padding: '16px', backgroundColor: 'white', borderRadius: '8px', border: '1px solid #e0e0e0' }}>
        <div style={{ display: 'flex', gap: '8px' }}>
          <input
            type="text"
            placeholder="Search organizations..."
            style={{ flex: 1, padding: '8px', borderRadius: '4px', border: '1px solid #e0e0e0' }}
          />
          <button style={{ padding: '8px 16px', borderRadius: '4px', border: '1px solid #e0e0e0', backgroundColor: 'white', cursor: 'pointer' }}>
            Search
          </button>
        </div>
      </div>

      <div style={{ display: 'flex', gap: '16px', flexWrap: 'wrap' }}>
        {[
          { name: 'Acme Inc', members: 45, billing: 'Pro', quota: '1M requests', status: 'Active' },
          { name: 'Tech Corp', members: 120, billing: 'Enterprise', quota: '10M requests', status: 'Active' },
          { name: 'Startup XYZ', members: 8, billing: 'Free', quota: '10K requests', status: 'Active' },
          { name: 'Global Solutions', members: 250, billing: 'Enterprise', quota: 'Unlimited', status: 'Suspended' },
        ].map((org, i) => (
          <div key={i} style={{ flex: 1, minWidth: 300, padding: '16px', backgroundColor: 'white', borderRadius: '8px', border: '1px solid #e0e0e0' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '8px' }}>
              <h3 style={{ fontSize: '16px', fontWeight: 'bold', margin: 0 }}>{org.name}</h3>
              <span style={{ fontSize: '12px', color: org.status === 'Active' ? '#4caf50' : '#f44336' }}>{org.status}</span>
            </div>

            <div style={{ display: 'flex', gap: '16px', marginTop: '8px' }}>
              <div style={{ flex: 1 }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
                  <span>👥</span>
                  <span style={{ fontSize: '12px' }}>Members</span>
                </div>
                <p style={{ fontSize: '14px', fontWeight: 'bold', margin: '4px 0 0 0' }}>{org.members}</p>
              </div>

              <div style={{ flex: 1 }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
                  <span>💰</span>
                  <span style={{ fontSize: '12px' }}>Plan</span>
                </div>
                <p style={{ fontSize: '14px', fontWeight: 'bold', margin: '4px 0 0 0' }}>{org.billing}</p>
              </div>

              <div style={{ flex: 1 }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
                  <span>💾</span>
                  <span style={{ fontSize: '12px' }}>Quota</span>
                </div>
                <p style={{ fontSize: '14px', fontWeight: 'bold', margin: '4px 0 0 0' }}>{org.quota}</p>
              </div>
            </div>

            <div style={{ display: 'flex', gap: '8px', marginTop: '16px' }}>
              <button style={{ flex: 1, padding: '8px', borderRadius: '4px', border: '1px solid #e0e0e0', backgroundColor: 'white', cursor: 'pointer' }}>
                View Details
              </button>
              <button style={{ flex: 1, padding: '8px', borderRadius: '4px', border: '1px solid #e0e0e0', backgroundColor: 'white', cursor: 'pointer' }}>
                Manage
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}
