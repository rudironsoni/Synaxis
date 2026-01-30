import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Shield, Eye, EyeOff } from 'lucide-react'
import useSettingsStore from '@/stores/settings'

export default function AdminLogin() {
  const [token, setToken] = useState('')
  const [showToken, setShowToken] = useState(false)
  const [error, setError] = useState('')
  const navigate = useNavigate()
  const setJwtToken = useSettingsStore((s: any) => s.setJwtToken)

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    setError('')

    if (!token.trim()) {
      setError('Please enter a JWT token')
      return
    }

    try {
      const parts = token.split('.')
      if (parts.length !== 3) {
        setError('Invalid JWT token format')
        return
      }

      const payload = JSON.parse(atob(parts[1]))
      if (!payload.sub && !payload.email) {
        setError('Token missing required claims')
        return
      }

      setJwtToken(token)
      navigate('/admin')
    } catch (err) {
      setError('Invalid JWT token')
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-[var(--background)]">
      <div className="w-full max-w-md p-8 bg-[var(--card)] rounded-lg border border-[var(--border)] shadow-lg">
        <div className="flex flex-col items-center mb-8">
          <Shield className="w-16 h-16 text-[var(--primary)] mb-4" />
          <h1 className="text-2xl font-bold text-[var(--foreground)]">Synaxis Admin</h1>
          <p className="text-sm text-[var(--muted-foreground)] mt-2">
            Enter your JWT token to access the admin panel
          </p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-6">
          <div>
            <label htmlFor="token" className="block text-sm font-medium text-[var(--foreground)] mb-2">
              JWT Token
            </label>
            <div className="relative">
              <input
                id="token"
                type={showToken ? 'text' : 'password'}
                value={token}
                onChange={(e) => setToken(e.target.value)}
                placeholder="eyJhbGciOiJIUzI1NiIs..."
                className="w-full px-4 py-3 pr-12 bg-[var(--input)] border border-[var(--border)] rounded-lg text-[var(--foreground)] placeholder:text-[var(--muted-foreground)] focus:outline-none focus:ring-2 focus:ring-[var(--primary)]"
              />
              <button
                type="button"
                onClick={() => setShowToken(!showToken)}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-[var(--muted-foreground)] hover:text-[var(--foreground)]"
                aria-label={showToken ? 'Hide token' : 'Show token'}
              >
                {showToken ? <EyeOff className="w-5 h-5" /> : <Eye className="w-5 h-5" />}
              </button>
            </div>
            {error && (
              <p className="mt-2 text-sm text-red-500">{error}</p>
            )}
          </div>

          <button
            type="submit"
            className="w-full py-3 bg-[var(--primary)] text-[var(--primary-foreground)] font-semibold rounded-lg hover:opacity-90 transition-opacity"
          >
            Access Admin Panel
          </button>
        </form>

        <div className="mt-6 text-center">
          <a
            href="/"
            className="text-sm text-[var(--muted-foreground)] hover:text-[var(--foreground)] transition-colors"
          >
            Back to Chat
          </a>
        </div>
      </div>
    </div>
  )
}
