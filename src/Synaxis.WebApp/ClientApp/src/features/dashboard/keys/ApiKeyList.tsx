import { useState } from 'react'
import Button from '@/components/ui/Button'
import Modal from '@/components/ui/Modal'
import KeyCreationModal from './KeyCreationModal'

interface ApiKey {
  id: string
  name: string
  key: string
  createdAt: Date
  status: 'active' | 'revoked'
}

interface ApiKeyListProps {
  onKeyRevoked?: (id: string) => void
  onKeyCreated?: (key: ApiKey) => void
}

const maskKey = (key: string): string => {
  return key.replace(/.(?=.{4})/g, '•')
}

const formatDate = (date: Date): string => {
  return new Date(date).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}

export default function ApiKeyList({ onKeyRevoked, onKeyCreated }: ApiKeyListProps) {
  const [keys, setKeys] = useState<ApiKey[]>([
    {
      id: '1',
      name: 'Production Key',
      key: 'sk-1234567890abcdefghijklmnop1234567890',
      createdAt: new Date('2024-01-15'),
      status: 'active',
    },
    {
      id: '2',
      name: 'Development Key',
      key: 'sk-0987654321zyxwvutsrqponmlkjihgfedcba',
      createdAt: new Date('2024-02-01'),
      status: 'active',
    },
  ])

  const [showCreationModal, setShowCreationModal] = useState(false)
  const [newKeyDisplay, setNewKeyDisplay] = useState<{ name: string; key: string } | null>(null)
  const [showNewKeyModal, setShowNewKeyModal] = useState(false)

  const handleCreateKey = (name: string) => {
    // Generate a mock API key
    const newKey = `sk-${Math.random().toString(36).substring(2, 38)}`
    const apiKey: ApiKey = {
      id: String(keys.length + 1),
      name,
      key: newKey,
      createdAt: new Date(),
      status: 'active',
    }

    // Show the key once before masking
    setNewKeyDisplay({ name, key: newKey })
    setShowNewKeyModal(true)

    // Add to list (masked after modal closes)
    setTimeout(() => {
      setKeys([...keys, apiKey])
      onKeyCreated?.(apiKey)
    }, 100)

    setShowCreationModal(false)
  }

  const handleRevokeKey = (id: string) => {
    setKeys(keys.filter((k) => k.id !== id))
    onKeyRevoked?.(id)
  }

  const handleCopyKey = (key: string) => {
    navigator.clipboard.writeText(key)
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold">API Key Management</h2>
          <p className="text-sm text-[var(--muted-foreground)]">
            Manage your API keys and access tokens.
          </p>
        </div>
        <Button variant="primary" onClick={() => setShowCreationModal(true)}>
          Create New Key
        </Button>
      </div>

      {keys.length === 0 ? (
        <div className="p-8 border border-[var(--border)] rounded-lg text-center">
          <p className="text-[var(--muted-foreground)]">No API keys yet. Create one to get started.</p>
        </div>
      ) : (
        <div className="space-y-4">
          {keys.map((apiKey) => (
            <div
              key={apiKey.id}
              className="flex items-center justify-between p-4 border border-[var(--border)] rounded-lg hover:bg-[var(--accent)]/50 transition-colors"
            >
              <div className="flex-1">
                <p className="font-medium text-[var(--foreground)]">{apiKey.name}</p>
                <div className="flex items-center gap-2 mt-1">
                  <code className="text-xs bg-[var(--muted)] text-[var(--muted-foreground)] px-2 py-1 rounded font-mono">
                    {maskKey(apiKey.key)} (masked)
                  </code>
                  <span className="text-xs text-[var(--muted-foreground)]">
                    Created {formatDate(apiKey.createdAt)}
                  </span>
                </div>
                <div className="mt-2">
                  <span
                    className={`inline-block text-xs px-2 py-1 rounded ${
                      apiKey.status === 'active'
                        ? 'bg-green-500/20 text-green-700'
                        : 'bg-red-500/20 text-red-700'
                    }`}
                  >
                    {apiKey.status === 'active' ? 'Active' : 'Revoked'}
                  </span>
                </div>
              </div>

              <div className="flex gap-2">
                <Button
                  variant="ghost"
                  onClick={() => handleCopyKey(apiKey.key)}
                  title="Copy full key to clipboard"
                >
                  Copy
                </Button>
                <Button
                  variant="danger"
                  onClick={() => handleRevokeKey(apiKey.id)}
                  title="Revoke this API key"
                >
                  Revoke
                </Button>
              </div>
            </div>
          ))}
        </div>
      )}

      <KeyCreationModal
        open={showCreationModal}
        onClose={() => setShowCreationModal(false)}
        onCreate={handleCreateKey}
      />

      {/* Show new key display modal */}
      <Modal open={showNewKeyModal} onClose={() => setShowNewKeyModal(false)} title="API Key Created">
        <div className="space-y-4">
          <p className="text-sm text-[var(--muted-foreground)]">
            Your API key has been created. Copy it now as you won't be able to see it again.
          </p>
          <div className="bg-[var(--muted)] p-3 rounded-lg">
            <p className="text-xs text-[var(--muted-foreground)] mb-2">Key Name:</p>
            <p className="font-mono text-sm text-[var(--foreground)] break-all">{newKeyDisplay?.name}</p>
          </div>
          <div className="bg-[var(--muted)] p-3 rounded-lg">
            <p className="text-xs text-[var(--muted-foreground)] mb-2">API Key:</p>
            <p className="font-mono text-sm text-[var(--foreground)] break-all">{newKeyDisplay?.key}</p>
          </div>
          <div className="flex gap-2">
            <Button
              variant="primary"
              onClick={() => {
                if (newKeyDisplay?.key) {
                  handleCopyKey(newKeyDisplay.key)
                }
              }}
              className="flex-1"
            >
              Copy Key
            </Button>
            <Button
              variant="ghost"
              onClick={() => setShowNewKeyModal(false)}
              className="flex-1"
            >
              Done
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  )
}
