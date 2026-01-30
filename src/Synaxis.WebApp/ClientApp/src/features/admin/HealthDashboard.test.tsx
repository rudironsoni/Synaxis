import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import HealthDashboard from './HealthDashboard';

const mockFetch = vi.fn();
global.fetch = mockFetch;

const mockJwtToken = 'test-jwt-token';

vi.mock('@/stores/settings', () => ({
  default: (selector: any) => selector({
    jwtToken: mockJwtToken,
  }),
}));

describe('HealthDashboard', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  const mockHealthData = {
    services: [
      { name: 'PostgreSQL', status: 'healthy', latency: 15, lastChecked: new Date().toISOString() },
      { name: 'Redis', status: 'healthy', latency: 5, lastChecked: new Date().toISOString() },
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
        status: 'offline', 
        lastChecked: new Date().toISOString(),
        errorMessage: 'Connection timeout'
      },
    ],
    overallStatus: 'degraded',
    timestamp: new Date().toISOString(),
  };

  it('should render health dashboard header', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => mockHealthData,
    });

    render(<HealthDashboard />);

    await waitFor(() => {
      expect(screen.getByText('Health Dashboard')).toBeInTheDocument();
      expect(screen.getByText('Monitor the status of all providers and services in real-time.')).toBeInTheDocument();
    });
  });

  it('should fetch health data on mount', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => mockHealthData,
    });

    render(<HealthDashboard />);

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledWith('/admin/health', {
        headers: {
          'Authorization': 'Bearer test-jwt-token',
        },
      });
    });
  });

  it('should display services section', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => mockHealthData,
    });

    render(<HealthDashboard />);

    await waitFor(() => {
      expect(screen.getByText('Services')).toBeInTheDocument();
      expect(screen.getByText('PostgreSQL')).toBeInTheDocument();
      expect(screen.getByText('Redis')).toBeInTheDocument();
    });
  });

  it('should display providers section', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => mockHealthData,
    });

    render(<HealthDashboard />);

    await waitFor(() => {
      expect(screen.getByText('AI Providers')).toBeInTheDocument();
      expect(screen.getByText('Groq')).toBeInTheDocument();
      expect(screen.getByText('Cohere')).toBeInTheDocument();
    });
  });

  it('should show overall system status', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => mockHealthData,
    });

    render(<HealthDashboard />);

    await waitFor(() => {
      expect(screen.getByText('Overall System Status')).toBeInTheDocument();
      expect(screen.getByText('Some services experiencing issues')).toBeInTheDocument();
    });
  });

  it('should display latency for services', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => mockHealthData,
    });

    render(<HealthDashboard />);

    await waitFor(() => {
      expect(screen.getByText('15ms')).toBeInTheDocument();
      expect(screen.getByText('5ms')).toBeInTheDocument();
    });
  });

  it('should display success rate for providers', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => mockHealthData,
    });

    render(<HealthDashboard />);

    await waitFor(() => {
      expect(screen.getByText('Success Rate')).toBeInTheDocument();
      expect(screen.getByText('98.5%')).toBeInTheDocument();
    });
  });

  it('should show error messages for failed providers', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => mockHealthData,
    });

    render(<HealthDashboard />);

    await waitFor(() => {
      expect(screen.getByText('Connection timeout')).toBeInTheDocument();
    });
  });

  it('should show status indicators', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => mockHealthData,
    });

    render(<HealthDashboard />);

    await waitFor(() => {
      const healthyElements = screen.getAllByText('healthy');
      expect(healthyElements.length).toBeGreaterThanOrEqual(1);
      expect(screen.getByText('online')).toBeInTheDocument();
      expect(screen.getByText('offline')).toBeInTheDocument();
    });
  });

  it('should show last updated timestamp', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => mockHealthData,
    });

    render(<HealthDashboard />);

    await waitFor(() => {
      expect(screen.getByText(/Last updated:/)).toBeInTheDocument();
    });
  });

  it('should refresh data on button click', async () => {
    mockFetch.mockResolvedValue({
      ok: true,
      json: async () => mockHealthData,
    });

    render(<HealthDashboard />);

    await waitFor(() => {
      expect(screen.getByText('Groq')).toBeInTheDocument();
    });

    const refreshButton = screen.getByText('Refresh');
    fireEvent.click(refreshButton);

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledTimes(2);
    });
  });

  it('should toggle auto-refresh', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => mockHealthData,
    });

    render(<HealthDashboard />);

    await waitFor(() => {
      expect(screen.getByText('Auto')).toBeInTheDocument();
    });

    const autoRefreshButton = screen.getByText('Auto');
    fireEvent.click(autoRefreshButton);

    await waitFor(() => {
      expect(screen.getByText('Paused')).toBeInTheDocument();
    });
  });

  it('should show loading state initially', () => {
    mockFetch.mockImplementation(() => new Promise(() => {}));

    render(<HealthDashboard />);

    expect(document.querySelector('.animate-spin')).toBeInTheDocument();
  });

  it('should handle fetch error gracefully', async () => {
    mockFetch.mockRejectedValueOnce(new Error('Network error'));

    render(<HealthDashboard />);

    await waitFor(() => {
      expect(screen.getByText(/Network error/)).toBeInTheDocument();
    });

    expect(screen.getByText('PostgreSQL')).toBeInTheDocument();
    expect(screen.getByText('Groq')).toBeInTheDocument();
  });

  it('should show auto-refresh information', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => mockHealthData,
    });

    render(<HealthDashboard />);

    await waitFor(() => {
      expect(screen.getByText(/Auto-refresh every 10 seconds/)).toBeInTheDocument();
    });
  });
});
