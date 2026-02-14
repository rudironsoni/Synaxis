import { createFileRoute } from '@tanstack/react-router'

export const Route = createFileRoute('/users')({
  component: Users,
})

function Users() {
  return (
    <div style={{ padding: '16px', gap: '16px' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <h1 style={{ fontSize: '24px', fontWeight: 'bold', margin: 0 }}>User Management</h1>
        <button style={{ padding: '8px 16px', borderRadius: '4px', border: '1px solid #e0e0e0', backgroundColor: 'white', cursor: 'pointer' }}>
          Export
        </button>
      </div>

      <div style={{ padding: '16px', backgroundColor: 'white', borderRadius: '8px', border: '1px solid #e0e0e0' }}>
        <div style={{ display: 'flex', gap: '8px' }}>
          <input
            type="text"
            placeholder="Search users..."
            style={{ flex: 1, padding: '8px', borderRadius: '4px', border: '1px solid #e0e0e0' }}
          />
          <button style={{ padding: '8px 16px', borderRadius: '4px', border: '1px solid #e0e0e0', backgroundColor: 'white', cursor: 'pointer' }}>
            Search
          </button>
        </div>
      </div>

      <div style={{ padding: '16px', backgroundColor: 'white', borderRadius: '8px', border: '1px solid #e0e0e0' }}>
        <div style={{ display: 'flex', gap: '16px', marginBottom: '16px', borderBottom: '1px solid #e0e0e0', paddingBottom: '8px' }}>
          <div style={{ flex: 1, fontWeight: 'bold' }}>User</div>
          <div style={{ flex: 1, fontWeight: 'bold' }}>Email</div>
          <div style={{ flex: 1, fontWeight: 'bold' }}>Organization</div>
          <div style={{ flex: 1, fontWeight: 'bold' }}>Status</div>
          <div style={{ flex: 1, fontWeight: 'bold' }}>Requests</div>
          <div style={{ width: 100, fontWeight: 'bold' }}>Actions</div>
        </div>

        {[
          { name: 'John Doe', email: 'john@example.com', org: 'Acme Inc', status: 'Active', requests: 1234 },
          { name: 'Jane Smith', email: 'jane@example.com', org: 'Tech Corp', status: 'Active', requests: 5678 },
          { name: 'Bob Wilson', email: 'bob@example.com', org: 'Startup XYZ', status: 'Suspended', requests: 890 },
        ].map((user, i) => (
          <div key={i} style={{ display: 'flex', gap: '16px', padding: '8px 0', borderBottom: '1px solid #e0e0e0' }}>
            <div style={{ flex: 1 }}>{user.name}</div>
            <div style={{ flex: 1 }}>{user.email}</div>
            <div style={{ flex: 1 }}>{user.org}</div>
            <div style={{ flex: 1, color: user.status === 'Active' ? '#4caf50' : '#f44336' }}>{user.status}</div>
            <div style={{ flex: 1 }}>{user.requests.toLocaleString()}</div>
            <div style={{ width: 100, display: 'flex', gap: '4px' }}>
              <button style={{ padding: '4px 8px', borderRadius: '4px', border: '1px solid #e0e0e0', backgroundColor: 'white', cursor: 'pointer', fontSize: '12px' }}>View</button>
              <button style={{ padding: '4px 8px', borderRadius: '4px', border: '1px solid #f44336', backgroundColor: 'white', cursor: 'pointer', fontSize: '12px', color: '#f44336' }}>Ban</button>
            </div>
          </div>
        ))}
      </div>

      <div style={{ display: 'flex', justifyContent: 'center', gap: '8px' }}>
        <button disabled style={{ padding: '8px 16px', borderRadius: '4px', border: '1px solid #e0e0e0', backgroundColor: '#f5f5f5', cursor: 'not-allowed' }}>Previous</button>
        <button style={{ padding: '8px 16px', borderRadius: '4px', border: '1px solid #e0e0e0', backgroundColor: 'white', cursor: 'pointer' }}>1</button>
        <button style={{ padding: '8px 16px', borderRadius: '4px', border: '1px solid #e0e0e0', backgroundColor: 'white', cursor: 'pointer' }}>2</button>
        <button style={{ padding: '8px 16px', borderRadius: '4px', border: '1px solid #e0e0e0', backgroundColor: 'white', cursor: 'pointer' }}>3</button>
        <button style={{ padding: '8px 16px', borderRadius: '4px', border: '1px solid #e0e0e0', backgroundColor: 'white', cursor: 'pointer' }}>Next</button>
      </div>
    </div>
  )
}
