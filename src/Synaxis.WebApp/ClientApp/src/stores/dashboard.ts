import { create } from 'zustand'
import { devtools } from 'zustand/middleware'
import type { Provider, ProviderDetail, ProviderStatus, ProviderUsage } from '@/services/mockProviderService'
import type { AnalyticsSummary, AnalyticsEvent, TimeSeriesData, ProviderComparison } from '@/services/mockAnalyticsService'
import type { CanonicalModel, AliasConfiguration, GatewayConfiguration } from '@/services/mockConfigService'

interface DashboardState {
  providers: Provider[]
  selectedProvider: ProviderDetail | null
  providerStatus: Record<string, ProviderStatus>
  providerUsage: Record<string, ProviderUsage>
  analytics: AnalyticsSummary | null
  events: AnalyticsEvent[]
  timeSeries: TimeSeriesData[]
  providerComparison: ProviderComparison[]
  models: CanonicalModel[]
  aliases: AliasConfiguration[]
  gatewayConfig: GatewayConfiguration | null
  loading: boolean
  error: string | null
}

interface DashboardActions {
  setProviders: (providers: Provider[]) => void
  setSelectedProvider: (provider: ProviderDetail | null) => void
  setProviderStatus: (id: string, status: ProviderStatus) => void
  setProviderUsage: (id: string, usage: ProviderUsage) => void
  setAnalytics: (analytics: AnalyticsSummary) => void
  setEvents: (events: AnalyticsEvent[]) => void
  setTimeSeries: (data: TimeSeriesData[]) => void
  setProviderComparison: (comparison: ProviderComparison[]) => void
  setModels: (models: CanonicalModel[]) => void
  setAliases: (aliases: AliasConfiguration[]) => void
  setGatewayConfig: (config: GatewayConfiguration) => void
  updateModel: (id: string, model: Partial<CanonicalModel>) => void
  updateAlias: (id: string, alias: Partial<AliasConfiguration>) => void
  setLoading: (loading: boolean) => void
  setError: (error: string | null) => void
  clearError: () => void
}

export const useDashboardStore = create<DashboardState & DashboardActions>()(
  devtools(
    (set) => ({
      providers: [],
      selectedProvider: null,
      providerStatus: {},
      providerUsage: {},
      analytics: null,
      events: [],
      timeSeries: [],
      providerComparison: [],
      models: [],
      aliases: [],
      gatewayConfig: null,
      loading: false,
      error: null,
      setProviders: (providers) => set({ providers }),
      setSelectedProvider: (selectedProvider) => set({ selectedProvider }),
      setProviderStatus: (id, status) => 
        set((state) => ({ 
          providerStatus: { ...state.providerStatus, [id]: status } 
        })),
      setProviderUsage: (id, usage) => 
        set((state) => ({ 
          providerUsage: { ...state.providerUsage, [id]: usage } 
        })),
      setAnalytics: (analytics) => set({ analytics }),
      setEvents: (events) => set({ events }),
      setTimeSeries: (timeSeries) => set({ timeSeries }),
      setProviderComparison: (providerComparison) => set({ providerComparison }),
      setModels: (models) => set({ models }),
      setAliases: (aliases) => set({ aliases }),
      setGatewayConfig: (gatewayConfig) => set({ gatewayConfig }),
      updateModel: (id, model) =>
        set((state) => ({
          models: state.models.map((m) =>
            m.id === id ? { ...m, ...model } : m
          ),
        })),
      updateAlias: (id, alias) =>
        set((state) => ({
          aliases: state.aliases.map((a) =>
            a.id === id ? { ...a, ...alias } : a
          ),
        })),
      setLoading: (loading) => set({ loading }),
      setError: (error) => set({ error }),
      clearError: () => set({ error: null }),
    }),
    { name: 'dashboard-store' }
  )
)
