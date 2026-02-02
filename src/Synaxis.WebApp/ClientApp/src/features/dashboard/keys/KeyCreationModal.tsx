import React, { useState } from 'react'
import Modal from '@/components/ui/Modal'
import Input from '@/components/ui/Input'
import Button from '@/components/ui/Button'

interface KeyCreationModalProps {
  open: boolean
  onClose: () => void
  onCreate: (name: string) => void
}

export default function KeyCreationModal({ open, onClose, onCreate }: KeyCreationModalProps) {
  const [name, setName] = useState('')
  const [error, setError] = useState('')

  const handleCreate = () => {
    if (!name.trim()) {
      setError('Key name is required')
      return
    }

    if (name.length > 100) {
      setError('Key name must be less than 100 characters')
      return
    }

    onCreate(name)
    setName('')
    setError('')
  }

  const handleClose = () => {
    setName('')
    setError('')
    onClose()
  }

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      handleCreate()
    }
  }

  return (
    <Modal open={open} onClose={handleClose} title="Create API Key">
      <div className="space-y-4">
        <div>
          <label htmlFor="key-name-input" className="block text-sm font-medium text-[var(--foreground)] mb-2">
            Key Name
          </label>
          <Input
            id="key-name-input"
            type="text"
            placeholder="e.g., Production API, Development Key"
            value={name}
            onChange={(e) => {
              setName(e.target.value)
              if (error) setError('')
            }}
            onKeyPress={handleKeyPress}
            disabled={false}
            maxLength={100}
          />
          {error && <p className="text-xs text-red-600 mt-2">{error}</p>}
          <p className="text-xs text-[var(--muted-foreground)] mt-1">
            {name.length}/100 characters
          </p>
        </div>

        <div className="bg-blue-500/10 border border-blue-500/30 rounded-lg p-3">
          <p className="text-xs text-blue-700 font-medium">Security Notice</p>
          <p className="text-xs text-blue-600 mt-1">
            Your API key will be displayed only once after creation. Save it somewhere safe.
          </p>
        </div>

        <div className="flex gap-2 justify-end">
          <Button variant="ghost" onClick={handleClose}>
            Cancel
          </Button>
          <Button variant="primary" onClick={handleCreate} disabled={!name.trim()}>
            Create Key
          </Button>
        </div>
      </div>
    </Modal>
  )
}
