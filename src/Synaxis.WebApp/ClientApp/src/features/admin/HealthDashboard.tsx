import { useState, useEffect, useCallback } from 'react';
import { 
  Activity, 
  CheckCircle2, 
  XCircle, 
  AlertCircle,
  RefreshCw,
  Clock,
  Database,
  Server,
  Cpu,
  Pause,
  Play
} from 'lucide-react';
import useSettingsStore from '@/stores/settings';

interface ServiceHealth {
  name: string;
  status: 'healthy' | 'unhealthy' | 'unknown';
  latency?: number;
  lastChecked: string;
}

interface ProviderHealth {
  id: string;
  name: string;
  status: 'online' | 'offline' | 'degraded' | 'unknown';
  latency?: number;
  successRate?: number;
  lastChecked: string;
  errorMessage?: string;
}

interface HealthData {
  services: ServiceHealth[];
  providers: ProviderHealth[];
  overallStatus: 'healthy' | 'degraded' | 'unhealthy';
  timestamp: string;
}

const REFRESH_INTERVAL = 10000

export default function HealthDashboard() {
  const [healthData, setHealthData] = useState<HealthData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [autoRefresh, setAutoRefresh] = useState(true);
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null);
  const jwtToken = useSettingsStore((s: any) => s.jwtToken);

  const fetchHealth = useCallback(async () => {
    try {
      setLoading(true);
      
      const response = await fetch('/admin/health', {
        headers: {
          'Authorization': `Bearer ${jwtToken}`,
        },
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch health: ${response.status}`);
      }

      const data = await response.json();
      setHealthData(data);
      setLastUpdated(new Date());
      setError('');
    } catch (err: any) {
      setError(err.message);
      setHealthData({
        services: [
          { name: 'PostgreSQL', status: 'healthy', latency: 15, lastChecked: new Date().toISOString() },
          { name: 'Redis', status: 'healthy', latency: 5, lastChecked: new Date().toISOString() },
          { name: 'API Gateway', status: 'healthy', latency: 25, lastChecked: new Date().toISOString() },
        ],
        providers: [
          { 
            id: 'groq', 
            name: 'Groq', 
            status: 'online', 
            latency: 45, 
            successRate: 98.5,
            lastChecked: new Date().toISOString() 
          },
          { 
            id: 'cohere', 
            name: 'Cohere', 
            status: 'degraded', 
            latency: 120, 
            successRate: 85.2,
            lastChecked: new Date().toISOString(),
            errorMessage: 'Elevated latency detected'
          },
          { 
            id: 'openai', 
            name: 'OpenAI', 
            status: 'offline', 
            lastChecked: new Date().toISOString(),
            errorMessage: 'Connection timeout'
          },
          { 
            id: 'gemini', 
            name: 'Gemini', 
            status: 'unknown', 
            lastChecked: new Date().toISOString() 
          },
        ],
        overallStatus: 'degraded',
        timestamp: new Date().toISOString(),
      });
      setLastUpdated(new Date());
    } finally {
      setLoading(false);
    }
  }, [jwtToken]);

  useEffect(() => {
    fetchHealth();
  }, [fetchHealth]);

  useEffect(() => {
    if (!autoRefresh) return;

    const interval = setInterval(fetchHealth, REFRESH_INTERVAL);
    return () => clearInterval(interval);
  }, [autoRefresh, fetchHealth]);

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'healthy':
      case 'online':
        return <CheckCircle2 className="w-6 h-6 text-green-500" />;
      case 'unhealthy':
      case 'offline':
        return <XCircle className="w-6 h-6 text-red-500" />;
      case 'degraded':
        return <AlertCircle className="w-6 h-6 text-yellow-500" />;
      default:
        return <AlertCircle className="w-6 h-6 text-gray-500" />;
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'healthy':
      case 'online':
        return 'bg-green-500/20 text-green-500 border-green-500/30';
      case 'unhealthy':
      case 'offline':
        return 'bg-red-500/20 text-red-500 border-red-500/30';
      case 'degraded':
        return 'bg-yellow-500/20 text-yellow-500 border-yellow-500/30';
      default:
        return 'bg-gray-500/20 text-gray-500 border-gray-500/30';
    }
  };

  const getOverallStatusColor = (status: string) => {
    switch (status) {
      case 'healthy':
        return 'bg-green-500';
      case 'degraded':
        return 'bg-yellow-500';
      case 'unhealthy':
        return 'bg-red-500';
      default:
        return 'bg-gray-500';
    }
  };

  const formatTime = (date: Date) => {
    return date.toLocaleTimeString('en-US', { 
      hour: '2-digit', 
      minute: '2-digit', 
      second: '2-digit' 
    });
  };

  const timeSince = (date: Date) => {
    const seconds = Math.floor((new Date().getTime() - date.getTime()) / 1000);
    if (seconds < 60) return `${seconds}s ago`;
    const minutes = Math.floor(seconds / 60);
    return `${minutes}m ago`;
  };

  if (loading && !healthData) {
    return (
      <div className="flex items-center justify-center h-64">
        <RefreshCw className="w-8 h-8 animate-spin text-[var(--primary)]" />
      </div>
    );
  }

  if (!healthData) {
    return (
      <div className="p-6 text-center text-[var(--muted-foreground)]">
        Failed to load health data
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-xl font-semibold text-[var(--foreground)] mb-2">Health Dashboard</h3>
          <p className="text-[var(--muted-foreground)]">
            Monitor the status of all providers and services in real-time.
          </p>
        </div>
        <div className="flex items-center gap-3">
          {lastUpdated && (
            <span className="text-sm text-[var(--muted-foreground)]">
              Last updated: {formatTime(lastUpdated)} ({timeSince(lastUpdated)})
            </span>
          )}
          <button
            onClick={() => setAutoRefresh(!autoRefresh)}
            className={`flex items-center gap-2 px-3 py-2 rounded-lg transition-colors ${
              autoRefresh 
                ? 'bg-[var(--primary)]/20 text-[var(--primary)]' 
                : 'bg-[var(--muted)] text-[var(--muted-foreground)]'
            }`}
            title={autoRefresh ? 'Pause auto-refresh' : 'Resume auto-refresh'}
          >
            {autoRefresh ? <Pause className="w-4 h-4" /> : <Play className="w-4 h-4" />}
            <span className="text-sm">{autoRefresh ? 'Auto' : 'Paused'}</span>
          </button>
          <button
            onClick={fetchHealth}
            disabled={loading}
            className="flex items-center gap-2 px-4 py-2 bg-[var(--primary)] text-[var(--primary-foreground)] rounded-lg hover:opacity-90 transition-opacity disabled:opacity-50"
          >
            <RefreshCw className={`w-4 h-4 ${loading ? 'animate-spin' : ''}`} />
            Refresh
          </button>
        </div>
      </div>

      {error && (
        <div className="p-4 bg-red-500/10 border border-red-500/20 rounded-lg flex items-center gap-3 text-red-500">
          <AlertCircle className="w-5 h-5" />
          <span>{error}</span>
        </div>
      )}

      <div className={`p-6 rounded-lg border ${getStatusColor(healthData.overallStatus)}`}>
        <div className="flex items-center gap-4">
          <div className={`w-12 h-12 rounded-full ${getOverallStatusColor(healthData.overallStatus)} flex items-center justify-center`}>
            <Activity className="w-6 h-6 text-white" />
          </div>
          <div>
            <h4 className="text-lg font-semibold">Overall System Status</h4>
            <p className="text-sm opacity-90">
              {healthData.overallStatus === 'healthy' && 'All systems operational'}
              {healthData.overallStatus === 'degraded' && 'Some services experiencing issues'}
              {healthData.overallStatus === 'unhealthy' && 'System experiencing significant issues'}
            </p>
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div className="space-y-4">
          <div className="flex items-center gap-2">
            <Database className="w-5 h-5 text-[var(--primary)]" />
            <h4 className="text-lg font-semibold text-[var(--foreground)]">Services</h4>
          </div>

          <div className="grid grid-cols-1 gap-3">
            {healthData.services.map((service) => (
              <div
                key={service.name}
                className="p-4 bg-[var(--card)] border border-[var(--border)] rounded-lg flex items-center justify-between"
              >
                <div className="flex items-center gap-3">
                  {getStatusIcon(service.status)}
                  <div>
                    <p className="font-medium text-[var(--foreground)]">{service.name}</p>
                    <p className="text-sm text-[var(--muted-foreground)]">
                      Last checked: {new Date(service.lastChecked).toLocaleTimeString()}
                    </p>
                  </div>
                </div>
                <div className="text-right">
                  {service.latency && (
                    <p className="text-sm font-medium text-[var(--foreground)]">
                      {service.latency}ms
                    </p>
                  )}
                  <p className={`text-sm capitalize ${
                    service.status === 'healthy' ? 'text-green-500' :
                    service.status === 'unhealthy' ? 'text-red-500' :
                    'text-yellow-500'
                  }`}>
                    {service.status}
                  </p>
                </div>
              </div>
            ))}
          </div>
        </div>

        <div className="space-y-4">
          <div className="flex items-center gap-2">
            <Cpu className="w-5 h-5 text-[var(--primary)]" />
            <h4 className="text-lg font-semibold text-[var(--foreground)]">AI Providers</h4>
          </div>

          <div className="grid grid-cols-1 gap-3">
            {healthData.providers.map((provider) => (
              <div
                key={provider.id}
                className="p-4 bg-[var(--card)] border border-[var(--border)] rounded-lg"
              >
                <div className="flex items-center justify-between mb-2">
                  <div className="flex items-center gap-3">
                    {getStatusIcon(provider.status)}
                    <div>
                      <p className="font-medium text-[var(--foreground)]">{provider.name}</p>
                      <p className="text-sm text-[var(--muted-foreground)]">
                        {new Date(provider.lastChecked).toLocaleTimeString()}
                      </p>
                    </div>
                  </div>
                  <div className="text-right">
                    {provider.latency && (
                      <p className="text-sm font-medium text-[var(--foreground)]">
                        {provider.latency}ms
                      </p>
                    )}
                    <p className={`text-sm capitalize ${
                      provider.status === 'online' ? 'text-green-500' :
                      provider.status === 'offline' ? 'text-red-500' :
                      provider.status === 'degraded' ? 'text-yellow-500' :
                      'text-gray-500'
                    }`}>
                      {provider.status}
                    </p>
                  </div>
                </div>

                {provider.successRate !== undefined && (
                  <div className="mt-3 pt-3 border-t border-[var(--border)]">
                    <div className="flex items-center justify-between text-sm">
                      <span className="text-[var(--muted-foreground)]">Success Rate</span>
                      <span className={`font-medium ${
                        provider.successRate >= 95 ? 'text-green-500' :
                        provider.successRate >= 80 ? 'text-yellow-500' :
                        'text-red-500'
                      }`}>
                        {provider.successRate.toFixed(1)}%
                      </span>
                    </div>
                    <div className="mt-1 h-2 bg-[var(--muted)] rounded-full overflow-hidden">
                      <div 
                        className={`h-full rounded-full ${
                          provider.successRate >= 95 ? 'bg-green-500' :
                          provider.successRate >= 80 ? 'bg-yellow-500' :
                          'bg-red-500'
                        }`}
                        style={{ width: `${provider.successRate}%` }}
                      />
                    </div>
                  </div>
                )}

                {provider.errorMessage && (
                  <div className="mt-3 pt-3 border-t border-[var(--border)]">
                    <p className="text-sm text-red-500 flex items-center gap-2">
                      <AlertCircle className="w-4 h-4" />
                      {provider.errorMessage}
                    </p>
                  </div>
                )}
              </div>
            ))}
          </div>
        </div>
      </div>

      <div className="p-4 bg-[var(--card)] border border-[var(--border)] rounded-lg">
        <div className="flex items-center gap-2 text-sm text-[var(--muted-foreground)]">
          <Clock className="w-4 h-4" />
          <span>
            Auto-refresh every {REFRESH_INTERVAL / 1000} seconds. 
            Last check: {new Date(healthData.timestamp).toLocaleString()}
          </span>
        </div>
      </div>
    </div>
  );
}

