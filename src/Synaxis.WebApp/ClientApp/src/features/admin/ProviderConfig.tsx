import { useState, useEffect } from 'react';
import { 
  Cpu, 
  CheckCircle2, 
  XCircle, 
  Key, 
  Server,
  ChevronDown,
  ChevronUp,
  RefreshCw,
  Save,
  AlertCircle
} from 'lucide-react';
import useSettingsStore from '@/stores/settings';

interface ProviderModel {
  id: string;
  name: string;
  enabled: boolean;
}

interface Provider {
  id: string;
  name: string;
  type: string;
  enabled: boolean;
  tier: number;
  endpoint?: string;
  keyConfigured: boolean;
  models: ProviderModel[];
  status: 'online' | 'offline' | 'unknown';
  latency?: number;
}

interface ProviderUpdate {
  enabled?: boolean;
  key?: string;
  endpoint?: string;
  tier?: number;
}

export default function ProviderConfig() {
  const [providers, setProviders] = useState<Provider[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [expandedProvider, setExpandedProvider] = useState<string | null>(null);
  const [saving, setSaving] = useState<string | null>(null);
  const [editingKey, setEditingKey] = useState<string | null>(null);
  const [keyInput, setKeyInput] = useState('');
  const jwtToken = useSettingsStore((s: any) => s.jwtToken);

  useEffect(() => {
    fetchProviders();
  }, []);

  const fetchProviders = async () => {
    try {
      setLoading(true);
      setError('');
      
      const response = await fetch('/admin/providers', {
        headers: {
          'Authorization': `Bearer ${jwtToken}`,
        },
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch providers: ${response.status}`);
      }

      const data = await response.json();
      setProviders(data);
    } catch (err: any) {
      setError(err.message);
      
      setProviders([
        {
          id: 'groq',
          name: 'Groq',
          type: 'groq',
          enabled: true,
          tier: 0,
          keyConfigured: true,
          models: [
            { id: 'llama-3.1-70b-versatile', name: 'Llama 3.1 70B', enabled: true },
            { id: 'llama-3.1-8b-instant', name: 'Llama 3.1 8B', enabled: true },
          ],
          status: 'online',
          latency: 45,
        },
        {
          id: 'cohere',
          name: 'Cohere',
          type: 'cohere',
          enabled: true,
          tier: 1,
          keyConfigured: false,
          models: [
            { id: 'command-r', name: 'Command R', enabled: true },
            { id: 'command-r-plus', name: 'Command R+', enabled: true },
          ],
          status: 'unknown',
        },
        {
          id: 'openai',
          name: 'OpenAI Compatible',
          type: 'openai',
          enabled: false,
          tier: 2,
          endpoint: 'https://api.openai.com/v1',
          keyConfigured: true,
          models: [
            { id: 'gpt-4', name: 'GPT-4', enabled: false },
            { id: 'gpt-3.5-turbo', name: 'GPT-3.5 Turbo', enabled: false },
          ],
          status: 'offline',
        },
      ]);
    } finally {
      setLoading(false);
    }
  };

  const updateProvider = async (providerId: string, updates: ProviderUpdate) => {
    try {
      setSaving(providerId);
      
      const response = await fetch(`/admin/providers/${providerId}`, {
        method: 'PUT',
        headers: {
          'Authorization': `Bearer ${jwtToken}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(updates),
      });

      if (!response.ok) {
        throw new Error(`Failed to update provider: ${response.status}`);
      }

      setProviders(prev => prev.map(p => 
        p.id === providerId 
          ? { ...p, ...updates, keyConfigured: updates.key ? true : p.keyConfigured }
          : p
      ));
      
      setEditingKey(null);
      setKeyInput('');
    } catch (err: any) {
      setError(err.message);
      
      setProviders(prev => prev.map(p => 
        p.id === providerId 
          ? { ...p, ...updates, keyConfigured: updates.key ? true : p.keyConfigured }
          : p
      ));
      setEditingKey(null);
      setKeyInput('');
    } finally {
      setSaving(null);
    }
  };

  const toggleProvider = (provider: Provider) => {
    updateProvider(provider.id, { enabled: !provider.enabled });
  };

  const saveKey = (providerId: string) => {
    if (keyInput.trim()) {
      updateProvider(providerId, { key: keyInput.trim() });
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'online':
        return <CheckCircle2 className="w-5 h-5 text-green-500" />;
      case 'offline':
        return <XCircle className="w-5 h-5 text-red-500" />;
      default:
        return <AlertCircle className="w-5 h-5 text-yellow-500" />;
    }
  };

  const getStatusText = (status: string) => {
    switch (status) {
      case 'online':
        return 'Online';
      case 'offline':
        return 'Offline';
      default:
        return 'Unknown';
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <RefreshCw className="w-8 h-8 animate-spin text-[var(--primary)]" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-xl font-semibold text-[var(--foreground)] mb-2">Provider Configuration</h3>
          <p className="text-[var(--muted-foreground)]">Manage AI provider settings, API keys, and model availability.</p>
        </div>
        <button
          onClick={fetchProviders}
          disabled={loading}
          className="flex items-center gap-2 px-4 py-2 bg-[var(--primary)] text-[var(--primary-foreground)] rounded-lg hover:opacity-90 transition-opacity disabled:opacity-50"
        >
          <RefreshCw className={`w-4 h-4 ${loading ? 'animate-spin' : ''}`} />
          Refresh
        </button>
      </div>

      {error && (
        <div className="p-4 bg-red-500/10 border border-red-500/20 rounded-lg flex items-center gap-3 text-red-500">
          <AlertCircle className="w-5 h-5" />
          <span>{error}</span>
        </div>
      )}

      <div className="space-y-4">
        {providers.map(provider => (
          <div
            key={provider.id}
            data-testid="provider-card"
            className="bg-[var(--card)] border border-[var(--border)] rounded-lg overflow-hidden"
          >
            <div
              className="p-4 flex items-center justify-between cursor-pointer hover:bg-[var(--muted)]/20 transition-colors"
              onClick={() => setExpandedProvider(expandedProvider === provider.id ? null : provider.id)}
            >
              <div className="flex items-center gap-4">
                <Cpu className="w-6 h-6 text-[var(--primary)]" />
                <div>
                  <h4 className="font-semibold text-[var(--foreground)]">{provider.name}</h4>
                  <div className="flex items-center gap-3 text-sm text-[var(--muted-foreground)]">
                    <span className="capitalize">{provider.type}</span>
                    <span>•</span>
                    <span>Tier {provider.tier}</span>
                    {provider.latency && (
                      <>
                        <span>•</span>
                        <span>{provider.latency}ms</span>
                      </>
                    )}
                  </div>
                </div>
              </div>

              <div className="flex items-center gap-4">
                <div className="flex items-center gap-2">
                  {getStatusIcon(provider.status)}
                  <span className={`text-sm ${
                    provider.status === 'online' ? 'text-green-500' : 
                    provider.status === 'offline' ? 'text-red-500' : 'text-yellow-500'
                  }`}>
                    {getStatusText(provider.status)}
                  </span>
                </div>

                <div className="flex items-center gap-2">
                  {provider.keyConfigured ? (
                    <div className="flex items-center gap-1.5 text-sm text-green-500">
                      <Key className="w-4 h-4" />
                      <span>Key set</span>
                    </div>
                  ) : (
                    <div className="flex items-center gap-1.5 text-sm text-yellow-500">
                      <Key className="w-4 h-4" />
                      <span>No key</span>
                    </div>
                  )}
                </div>

                <button
                  onClick={(e) => {
                    e.stopPropagation();
                    toggleProvider(provider);
                  }}
                  disabled={saving === provider.id}
                  className={`px-3 py-1.5 rounded text-sm font-medium transition-colors ${
                    provider.enabled
                      ? 'bg-green-500/20 text-green-500 hover:bg-green-500/30'
                      : 'bg-[var(--muted)] text-[var(--muted-foreground)] hover:bg-[var(--muted)]/80'
                  } disabled:opacity-50`}
                >
                  {saving === provider.id ? (
                    <RefreshCw className="w-4 h-4 animate-spin" />
                  ) : (
                    provider.enabled ? 'Enabled' : 'Disabled'
                  )}
                </button>

                {expandedProvider === provider.id ? (
                  <ChevronUp className="w-5 h-5 text-[var(--muted-foreground)]" />
                ) : (
                  <ChevronDown className="w-5 h-5 text-[var(--muted-foreground)]" />
                )}
              </div>
            </div>

            {expandedProvider === provider.id && (
              <div className="px-4 pb-4 border-t border-[var(--border)] bg-[var(--background)]/50">
                <div className="pt-4 space-y-4">
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div>
                      <label className="block text-sm font-medium text-[var(--foreground)] mb-2">
                        Provider ID
                      </label>
                      <input
                        type="text"
                        value={provider.id}
                        disabled
                        className="w-full px-3 py-2 bg-[var(--input)] border border-[var(--border)] rounded text-[var(--foreground)] disabled:opacity-50"
                      />
                    </div>

                    <div>
                      <label className="block text-sm font-medium text-[var(--foreground)] mb-2">
                        Type
                      </label>
                      <input
                        type="text"
                        value={provider.type}
                        disabled
                        className="w-full px-3 py-2 bg-[var(--input)] border border-[var(--border)] rounded text-[var(--foreground)] disabled:opacity-50"
                      />
                    </div>

                    {provider.endpoint && (
                      <div className="md:col-span-2">
                        <label className="block text-sm font-medium text-[var(--foreground)] mb-2">
                          Endpoint URL
                        </label>
                        <div className="flex gap-2">
                          <input
                            type="text"
                            value={provider.endpoint}
                            disabled
                            className="flex-1 px-3 py-2 bg-[var(--input)] border border-[var(--border)] rounded text-[var(--foreground)] disabled:opacity-50"
                          />
                        </div>
                      </div>
                    )}

                    <div className="md:col-span-2">
                      <label className="block text-sm font-medium text-[var(--foreground)] mb-2">
                        API Key
                      </label>
                      {editingKey === provider.id ? (
                        <div className="flex gap-2">
                          <input
                            type="password"
                            value={keyInput}
                            onChange={(e) => setKeyInput(e.target.value)}
                            placeholder="Enter API key..."
                            className="flex-1 px-3 py-2 bg-[var(--input)] border border-[var(--border)] rounded text-[var(--foreground)]"
                          />
                          <button
                            onClick={() => saveKey(provider.id)}
                            disabled={saving === provider.id || !keyInput.trim()}
                            className="px-4 py-2 bg-[var(--primary)] text-[var(--primary-foreground)] rounded hover:opacity-90 transition-opacity disabled:opacity-50 flex items-center gap-2"
                          >
                            {saving === provider.id ? (
                              <RefreshCw className="w-4 h-4 animate-spin" />
                            ) : (
                              <Save className="w-4 h-4" />
                            )}
                            Save
                          </button>
                          <button
                            onClick={() => {
                              setEditingKey(null);
                              setKeyInput('');
                            }}
                            className="px-4 py-2 bg-[var(--muted)] text-[var(--foreground)] rounded hover:bg-[var(--muted)]/80 transition-colors"
                          >
                            Cancel
                          </button>
                        </div>
                      ) : (
                        <div className="flex gap-2">
                          <input
                            type="password"
                            value={provider.keyConfigured ? '••••••••••••••••' : ''}
                            disabled
                            placeholder="No API key configured"
                            className="flex-1 px-3 py-2 bg-[var(--input)] border border-[var(--border)] rounded text-[var(--foreground)] disabled:opacity-50"
                          />
                          <button
                            onClick={() => {
                              setEditingKey(provider.id);
                              setKeyInput('');
                            }}
                            className="px-4 py-2 bg-[var(--primary)] text-[var(--primary-foreground)] rounded hover:opacity-90 transition-opacity flex items-center gap-2"
                          >
                            <Key className="w-4 h-4" />
                            {provider.keyConfigured ? 'Update Key' : 'Set Key'}
                          </button>
                        </div>
                      )}
                    </div>
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-[var(--foreground)] mb-2">
                      Available Models
                    </label>
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
                      {provider.models.map(model => (
                        <div
                          key={model.id}
                          data-testid="provider-model"
                          className="flex items-center justify-between p-3 bg-[var(--input)] border border-[var(--border)] rounded"
                        >
                          <div className="flex items-center gap-3">
                            <Server className="w-4 h-4 text-[var(--muted-foreground)]" />
                            <div>
                              <p className="text-sm font-medium text-[var(--foreground)]">{model.name}</p>
                              <p className="text-xs text-[var(--muted-foreground)]">{model.id}</p>
                            </div>
                          </div>
                          <div className={`px-2 py-1 rounded text-xs ${
                            model.enabled ? 'bg-green-500/20 text-green-500' : 'bg-[var(--muted)] text-[var(--muted-foreground)]'
                          }`}>
                            {model.enabled ? 'Active' : 'Inactive'}
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                </div>
              </div>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}
