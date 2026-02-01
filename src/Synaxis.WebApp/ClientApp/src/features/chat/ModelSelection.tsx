import { useEffect, useState } from 'react'
import { ChevronDown } from 'lucide-react'
import { defaultClient, type ModelDto } from '@/api/client'
import useSettingsStore from '@/stores/settings'

type ModelSelectionProps = {
  disabled?: boolean
}

export default function ModelSelection({ disabled }: ModelSelectionProps) {
  const [models, setModels] = useState<ModelDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const selectedModel = useSettingsStore((s) => s.selectedModel)
  const setSelectedModel = useSettingsStore((s) => s.setSelectedModel)

  useEffect(() => {
    const fetchModels = async () => {
      try {
        setLoading(true)
        setError(null)
        const response = await defaultClient.fetchModels()
        setModels(response.data)
      } catch (err) {
        console.error('Failed to fetch models:', err)
        setError(err instanceof Error ? err.message : 'Failed to fetch models')
      } finally {
        setLoading(false)
      }
    }

    fetchModels()
  }, [])

  const handleModelChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    setSelectedModel(e.target.value)
  }

  if (loading) {
    return (
      <div className="flex items-center gap-2 text-xs text-[var(--muted-foreground)]">
        <span>Loading models...</span>
      </div>
    )
  }

  if (error) {
    return (
      <div className="flex items-center gap-2 text-xs text-red-500">
        <span>Error: {error}</span>
      </div>
    )
  }

  if (models.length === 0) {
    return (
      <div className="flex items-center gap-2 text-xs text-[var(--muted-foreground)]">
        <span>No models available</span>
      </div>
    )
  }

  return (
    <div className="flex items-center gap-2">
      <label htmlFor="model-select" className="text-xs text-[var(--muted-foreground)]">
        Model:
      </label>
      <div className="relative">
        <select
          id="model-select"
          value={selectedModel}
          onChange={handleModelChange}
          disabled={disabled}
          className="appearance-none bg-[var(--input)] text-[var(--foreground)] border border-[var(--border)] rounded px-3 py-1.5 pr-8 text-sm disabled:opacity-50 disabled:cursor-not-allowed hover:border-[var(--primary)] focus:outline-none focus:ring-2 focus:ring-[var(--primary)]/50"
          aria-label="Select model"
        >
          {models.map((model) => (
            <option key={model.id} value={model.id}>
              {model.id} ({model.provider})
            </option>
          ))}
        </select>
        <ChevronDown className="absolute right-2 top-1/2 transform -translate-y-1/2 w-4 h-4 pointer-events-none text-[var(--muted-foreground)]" />
      </div>
    </div>
  )
}
