import { useEffect, useState } from 'react'
import Badge from '@/components/ui/Badge'
import { mockConfigService, type CanonicalModel } from '@/services/mockConfigService'

interface ModelListProps {
  onModelSelect?: (model: CanonicalModel) => void
  loading?: boolean
}

export default function ModelList({ onModelSelect, loading: externalLoading }: ModelListProps) {
  const [models, setModels] = useState<CanonicalModel[]>([])
  const [loading, setLoading] = useState<boolean>(externalLoading ?? true)
  const [error, setError] = useState<string | null>(null)
  const [selectedModelId, setSelectedModelId] = useState<string | null>(null)
  const [enabledModels, setEnabledModels] = useState<Set<string>>(new Set())

  useEffect(() => {
    const fetchModels = async () => {
      try {
        setLoading(true)
        const fetchedModels = await mockConfigService.getCanonicalModels()
        setModels(fetchedModels)
        // Initialize all models as enabled by default
        setEnabledModels(new Set(fetchedModels.map((m) => m.id)))
        setError(null)
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to fetch models')
        setModels([])
      } finally {
        setLoading(false)
      }
    }

    fetchModels()
  }, [])

  const handleModelSelect = (model: CanonicalModel) => {
    setSelectedModelId(model.id)
    onModelSelect?.(model)
  }

  const toggleModelEnabled = (modelId: string) => {
    setEnabledModels((prev) => {
      const newSet = new Set(prev)
      if (newSet.has(modelId)) {
        newSet.delete(modelId)
      } else {
        newSet.add(modelId)
      }
      return newSet
    })
  }

  if (error) {
    return (
      <div className="space-y-4">
        <div className="p-4 bg-red-50 border border-red-200 rounded-lg">
          <p className="text-red-700 text-sm">
            <span className="font-semibold">Error:</span> {error}
          </p>
        </div>
      </div>
    )
  }

  if (loading) {
    return (
      <div className="space-y-4">
        {[1, 2, 3].map((i) => (
          <div
            key={i}
            className="h-24 bg-[var(--muted)] border border-[var(--border)] rounded-lg animate-pulse"
          />
        ))}
      </div>
    )
  }

  if (models.length === 0) {
    return (
      <div className="p-8 border border-[var(--border)] rounded-lg text-center">
        <p className="text-[var(--muted-foreground)]">No models available</p>
      </div>
    )
  }

  return (
    <div className="space-y-3">
      {models.map((model) => {
        const isEnabled = enabledModels.has(model.id)
        const isSelected = selectedModelId === model.id

        return (
          <div
            key={model.id}
            onClick={() => handleModelSelect(model)}
            className={`flex items-center justify-between p-4 border rounded-lg cursor-pointer transition-all ${
              isSelected
                ? 'border-[var(--ring)] bg-[var(--accent)]'
                : 'border-[var(--border)] hover:border-[var(--ring)]'
            }`}
          >
            <div className="flex-1 min-w-0">
              <p className="font-semibold text-[var(--foreground)] truncate">{model.name}</p>
              <p className="text-sm text-[var(--muted-foreground)]">{model.provider}</p>
              {model.description && (
                <p className="text-xs text-[var(--muted-foreground)] mt-1 line-clamp-2">
                  {model.description}
                </p>
              )}
              <div className="flex flex-wrap gap-2 mt-2">
                {model.streaming && <Badge>Streaming</Badge>}
                {model.tools && <Badge>Tools</Badge>}
                {model.vision && <Badge>Vision</Badge>}
                {model.structuredOutput && <Badge>Structured</Badge>}
              </div>
            </div>

            <div className="flex items-center gap-2 ml-4 flex-shrink-0">
              <label className="flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  checked={isEnabled}
                  onChange={(e) => {
                    e.stopPropagation()
                    toggleModelEnabled(model.id)
                  }}
                  className="w-4 h-4 rounded cursor-pointer"
                />
                <span className="text-sm text-[var(--muted-foreground)] whitespace-nowrap">
                  {isEnabled ? 'Enabled' : 'Disabled'}
                </span>
              </label>
            </div>
          </div>
        )
      })}
    </div>
  )
}
