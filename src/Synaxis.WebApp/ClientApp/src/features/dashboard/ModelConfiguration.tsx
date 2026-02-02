import { useEffect, useState } from 'react'
import Badge from '@/components/ui/Badge'
import { mockConfigService, type CanonicalModel, type AliasConfiguration } from '@/services/mockConfigService'

interface FormData {
  priority: number
  description: string
  streamingEnabled: boolean
  toolsEnabled: boolean
  visionEnabled: boolean
  structuredOutputEnabled: boolean
}

export default function ModelConfiguration() {
  const [models, setModels] = useState<CanonicalModel[]>([])
  const [aliases, setAliases] = useState<AliasConfiguration[]>([])
  const [selectedModel, setSelectedModel] = useState<CanonicalModel | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [activeTab, setActiveTab] = useState<'models' | 'aliases'>('models')
  const [formData, setFormData] = useState<FormData>({
    priority: 0,
    description: '',
    streamingEnabled: false,
    toolsEnabled: false,
    visionEnabled: false,
    structuredOutputEnabled: false,
  })

  useEffect(() => {
    const fetchData = async () => {
      try {
        setLoading(true)
        const [modelsData, aliasesData] = await Promise.all([
          mockConfigService.getCanonicalModels(),
          mockConfigService.getAliases(),
        ])
        setModels(modelsData)
        setAliases(aliasesData)
        setError(null)
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load configuration')
      } finally {
        setLoading(false)
      }
    }

    fetchData()
  }, [])

  useEffect(() => {
    if (selectedModel) {
      setFormData({
        priority: 0,
        description: selectedModel.description || '',
        streamingEnabled: selectedModel.streaming,
        toolsEnabled: selectedModel.tools,
        visionEnabled: selectedModel.vision,
        structuredOutputEnabled: selectedModel.structuredOutput,
      })
    }
  }, [selectedModel])

  const handleModelSelect = (model: CanonicalModel) => {
    setSelectedModel(model)
  }

  const handleFormChange = (field: keyof FormData, value: FormData[keyof FormData]) => {
    setFormData((prev) => ({
      ...prev,
      [field]: value,
    }))
  }

  const handleSaveConfiguration = async () => {
    if (!selectedModel) return

    try {
      setSaving(true)
      await mockConfigService.updateCanonicalModel(selectedModel.id, {
        description: formData.description,
        streaming: formData.streamingEnabled,
        tools: formData.toolsEnabled,
        vision: formData.visionEnabled,
        structuredOutput: formData.structuredOutputEnabled,
      })

      setModels((prev) =>
        prev.map((m) =>
          m.id === selectedModel.id
            ? {
                ...m,
                description: formData.description,
                streaming: formData.streamingEnabled,
                tools: formData.toolsEnabled,
                vision: formData.visionEnabled,
                structuredOutput: formData.structuredOutputEnabled,
              }
            : m
        )
      )

      setSelectedModel({
        ...selectedModel,
        description: formData.description,
        streaming: formData.streamingEnabled,
        tools: formData.toolsEnabled,
        vision: formData.visionEnabled,
        structuredOutput: formData.structuredOutputEnabled,
      })

      alert('Configuration saved successfully!')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save configuration')
    } finally {
      setSaving(false)
    }
  }

  if (loading) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-bold">Model Configuration</h1>
        <div className="space-y-4">
          <div className="h-10 bg-[var(--muted)] rounded animate-pulse" />
          <div className="h-96 bg-[var(--muted)] rounded animate-pulse" />
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold">Model Configuration</h1>
        <p className="text-[var(--muted-foreground)] mt-1">
          Configure available models, set priorities, and manage model capabilities.
        </p>
      </div>

      {error && (
        <div className="p-4 bg-red-50 border border-red-200 rounded-lg">
          <p className="text-red-700 text-sm">
            <span className="font-semibold">Error:</span> {error}
          </p>
        </div>
      )}

      <div className="flex gap-4 border-b border-[var(--border)]">
        <button
          type="button"
          onClick={() => setActiveTab('models')}
          className={`px-4 py-2 font-medium transition-colors border-b-2 ${
            activeTab === 'models'
              ? 'border-[var(--ring)] text-[var(--foreground)]'
              : 'border-transparent text-[var(--muted-foreground)] hover:text-[var(--foreground)]'
          }`}
        >
          Models ({models.length})
        </button>
        <button
          type="button"
          onClick={() => setActiveTab('aliases')}
          className={`px-4 py-2 font-medium transition-colors border-b-2 ${
            activeTab === 'aliases'
              ? 'border-[var(--ring)] text-[var(--foreground)]'
              : 'border-transparent text-[var(--muted-foreground)] hover:text-[var(--foreground)]'
          }`}
        >
          Aliases ({aliases.length})
        </button>
      </div>

      {activeTab === 'models' && (
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div className="lg:col-span-1">
            <h3 className="font-semibold mb-3">Available Models</h3>
            <div className="space-y-2 max-h-[600px] overflow-y-auto">
              {models.map((model) => (
                <button
                  type="button"
                  key={model.id}
                  onClick={() => handleModelSelect(model)}
                  className={`w-full text-left p-3 rounded-lg border transition-colors ${
                    selectedModel?.id === model.id
                      ? 'border-[var(--ring)] bg-[var(--accent)]'
                      : 'border-[var(--border)] hover:border-[var(--ring)]'
                  }`}
                >
                  <p className="font-medium text-sm">{model.name}</p>
                  <p className="text-xs text-[var(--muted-foreground)]">{model.provider}</p>
                </button>
              ))}
            </div>
          </div>

          <div className="lg:col-span-2">
            {selectedModel ? (
              <div className="space-y-6 p-6 border border-[var(--border)] rounded-lg bg-[var(--card)]">
                <div>
                  <h3 className="text-lg font-semibold">{selectedModel.name}</h3>
                  <p className="text-sm text-[var(--muted-foreground)]">
                    Provider: <span className="font-medium">{selectedModel.provider}</span>
                  </p>
                </div>

                <div>
                  <p className="text-sm font-medium mb-2">Current Capabilities</p>
                  <div className="flex flex-wrap gap-2">
                    {selectedModel.streaming && <Badge>Streaming</Badge>}
                    {selectedModel.tools && <Badge>Tools</Badge>}
                    {selectedModel.vision && <Badge>Vision</Badge>}
                    {selectedModel.structuredOutput && <Badge>Structured Output</Badge>}
                    {!selectedModel.streaming &&
                      !selectedModel.tools &&
                      !selectedModel.vision &&
                      !selectedModel.structuredOutput && (
                        <p className="text-sm text-[var(--muted-foreground)]">No capabilities enabled</p>
                      )}
                  </div>
                </div>

                {selectedModel.contextWindow && (
                  <div className="grid grid-cols-2 gap-4 p-3 bg-[var(--muted)] rounded">
                    <div>
                      <p className="text-xs text-[var(--muted-foreground)]">Context Window</p>
                      <p className="font-medium">{selectedModel.contextWindow.toLocaleString()}</p>
                    </div>
                    <div>
                      <p className="text-xs text-[var(--muted-foreground)]">Max Tokens</p>
                      <p className="font-medium">{selectedModel.maxTokens?.toLocaleString() || 'N/A'}</p>
                    </div>
                  </div>
                )}

                <div className="space-y-4 border-t border-[var(--border)] pt-4">
                  <div>
                    <label htmlFor="description" className="block text-sm font-medium mb-1">
                      Description
                    </label>
                    <textarea
                      id="description"
                      value={formData.description}
                      onChange={(e) => handleFormChange('description', e.target.value)}
                      className="w-full px-3 py-2 border border-[var(--border)] rounded-lg focus:outline-none focus:ring-2 focus:ring-[var(--ring)] bg-[var(--background)]"
                      rows={3}
                      placeholder="Enter model description..."
                    />
                  </div>

                  <div>
                    <p className="text-sm font-medium mb-3">Capability Settings</p>
                    <div className="space-y-2">
                      {[
                        { key: 'streamingEnabled', label: 'Streaming Support' },
                        { key: 'toolsEnabled', label: 'Tools/Functions' },
                        { key: 'visionEnabled', label: 'Vision' },
                        { key: 'structuredOutputEnabled', label: 'Structured Output' },
                      ].map((capability) => (
                        <div key={capability.key} className="flex items-center gap-3">
                          <input
                            id={capability.key}
                            type="checkbox"
                            checked={formData[capability.key as keyof FormData] as boolean}
                            onChange={(e) =>
                              handleFormChange(
                                capability.key as keyof FormData,
                                e.target.checked as FormData[keyof FormData]
                              )
                            }
                            className="w-4 h-4 rounded cursor-pointer"
                          />
                          <label htmlFor={capability.key} className="text-sm cursor-pointer">
                            {capability.label}
                          </label>
                        </div>
                      ))}
                    </div>
                  </div>

                  <div>
                    <label htmlFor="priority" className="block text-sm font-medium mb-1">
                      Priority
                    </label>
                    <input
                      id="priority"
                      type="number"
                      value={formData.priority}
                      onChange={(e) => handleFormChange('priority', parseInt(e.target.value))}
                      className="w-full px-3 py-2 border border-[var(--border)] rounded-lg focus:outline-none focus:ring-2 focus:ring-[var(--ring)] bg-[var(--background)]"
                      min="0"
                      max="100"
                    />
                    <p className="text-xs text-[var(--muted-foreground)] mt-1">
                      Higher priority models are preferred in failover scenarios
                    </p>
                  </div>
                </div>

                <button
                  type="button"
                  onClick={handleSaveConfiguration}
                  disabled={saving}
                  className="w-full px-4 py-2 bg-[var(--ring)] text-white rounded-lg font-medium hover:bg-[var(--ring)]/90 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                >
                  {saving ? 'Saving...' : 'Save Configuration'}
                </button>
              </div>
            ) : (
              <div className="p-8 border border-[var(--border)] rounded-lg bg-[var(--muted)] text-center">
                <p className="text-[var(--muted-foreground)]">Select a model to configure</p>
              </div>
            )}
          </div>
        </div>
      )}

      {activeTab === 'aliases' && (
        <div>
          <h3 className="font-semibold mb-4">Model Aliases</h3>
          <div className="grid gap-4">
            {aliases.map((alias) => (
              <div key={alias.id} className="p-4 border border-[var(--border)] rounded-lg">
                <div className="flex items-start justify-between mb-3">
                  <div>
                    <p className="font-semibold capitalize">{alias.id}</p>
                    {alias.description && (
                      <p className="text-sm text-[var(--muted-foreground)]">{alias.description}</p>
                    )}
                  </div>
                  <Badge>Priority: {alias.priority}</Badge>
                </div>
                <div>
                  <p className="text-xs font-medium text-[var(--muted-foreground)] mb-2">Candidates:</p>
                  <div className="flex flex-wrap gap-2">
                    {alias.candidates.map((candidate) => (
                      <Badge key={candidate}>{candidate}</Badge>
                    ))}
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}
