import { Link } from '@tanstack/react-router'

const navItems = [
  { to: '/', label: 'Dashboard' },
  { to: '/users', label: 'Users' },
  { to: '/organizations', label: 'Organizations' },
  { to: '/models', label: 'Models' },
  { to: '/monitoring', label: 'Monitoring' },
  { to: '/settings', label: 'Settings' },
]

export function Sidebar() {
  return (
    <div style={{
      width: 250,
      backgroundColor: '#ffffff',
      borderRight: '1px solid #e0e0e0',
      padding: '16px',
      display: 'flex',
      flexDirection: 'column',
    }}>
      <div style={{ marginBottom: '24px' }}>
        <h2 style={{ fontSize: '20px', fontWeight: 'bold', margin: 0 }}>
          Synaxis Admin
        </h2>
      </div>

      <nav style={{ flex: 1 }}>
        {navItems.map((item) => (
          <Link
            key={item.to}
            to={item.to}
            activeProps={{ style: { backgroundColor: '#e3f2fd' } }}
            style={{
              display: 'flex',
              alignItems: 'center',
              padding: '12px',
              borderRadius: '8px',
              textDecoration: 'none',
              color: '#333',
              marginBottom: '8px',
            }}
          >
            {item.label}
          </Link>
        ))}
      </nav>

      <div style={{ borderTop: '1px solid #e0e0e0', paddingTop: '16px' }}>
        <button
          style={{
            width: '100%',
            padding: '12px',
            border: '1px solid #e0e0e0',
            backgroundColor: 'white',
            borderRadius: '8px',
            cursor: 'pointer',
          }}
        >
          Logout
        </button>
      </div>
    </div>
  )
}
