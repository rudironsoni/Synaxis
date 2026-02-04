import * as signalR from '@microsoft/signalr';

/**
 * Provider health update notification.
 */
export interface ProviderHealthUpdate {
  providerId: string;
  providerName: string;
  isHealthy: boolean;
  healthScore: number;
  averageLatencyMs: number;
  checkedAt: string;
}

/**
 * Cost optimization result notification.
 */
export interface CostOptimizationResult {
  organizationId: string;
  fromProvider: string;
  toProvider: string;
  reason: string;
  savingsPer1MTokens: number;
  appliedAt: string;
}

/**
 * Model discovery result notification.
 */
export interface ModelDiscoveryResult {
  modelId: string;
  canonicalId: string;
  displayName: string;
  providerName: string;
  isAvailableToOrganization: boolean;
}

/**
 * Security alert notification.
 */
export interface SecurityAlert {
  organizationId: string;
  alertType: string;
  severity: string;
  message: string;
  detectedAt: string;
}

/**
 * Audit event notification.
 */
export interface AuditEvent {
  id: string;
  action: string;
  entityType: string;
  performedBy: string;
  performedAt: string;
}

/**
 * Event handlers for real-time notifications.
 */
export interface RealtimeEventHandlers {
  onProviderHealthChanged?: (update: ProviderHealthUpdate) => void;
  onCostOptimizationApplied?: (result: CostOptimizationResult) => void;
  onModelDiscovered?: (result: ModelDiscoveryResult) => void;
  onSecurityAlert?: (alert: SecurityAlert) => void;
  onAuditEvent?: (event: AuditEvent) => void;
}

/**
 * Real-time service for Synaxis WebSocket updates using SignalR.
 * Provides real-time notifications for provider health, cost optimization,
 * model discovery, security alerts, and audit events.
 */
export class RealtimeService {
  private connection: signalR.HubConnection;
  private handlers: RealtimeEventHandlers;
  private isConnected: boolean = false;
  private currentOrganizationId?: string;

  /**
   * Create a new RealtimeService instance.
   * @param token JWT token for authentication
   * @param handlers Event handlers for real-time notifications
   * @param baseUrl Base URL for the API (defaults to current origin)
   */
  constructor(
    token: string,
    handlers: RealtimeEventHandlers = {},
    baseUrl?: string
  ) {
    this.handlers = handlers;
    
    const url = baseUrl ? `${baseUrl}/hubs/synaxis` : '/hubs/synaxis';
    
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(url, {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          // Exponential backoff: 0, 2, 10, 30 seconds, then 30 seconds
          if (retryContext.previousRetryCount === 0) {
            return 0;
          } else if (retryContext.previousRetryCount === 1) {
            return 2000;
          } else if (retryContext.previousRetryCount === 2) {
            return 10000;
          } else {
            return 30000;
          }
        },
      })
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.setupEventHandlers();
    this.setupConnectionHandlers();
  }

  /**
   * Connect to the SignalR hub and join organization group.
   * @param organizationId Organization ID to join
   */
  async connect(organizationId: string): Promise<void> {
    if (this.isConnected && this.currentOrganizationId === organizationId) {
      console.log('Already connected to organization', organizationId);
      return;
    }

    try {
      await this.connection.start();
      console.log('SignalR connection established');
      
      await this.connection.invoke('JoinOrganization', organizationId);
      console.log('Joined organization', organizationId);
      
      this.isConnected = true;
      this.currentOrganizationId = organizationId;
    } catch (err) {
      console.error('Error connecting to SignalR:', err);
      throw err;
    }
  }

  /**
   * Disconnect from the SignalR hub.
   */
  async disconnect(): Promise<void> {
    if (!this.isConnected) {
      return;
    }

    try {
      if (this.currentOrganizationId) {
        await this.connection.invoke('LeaveOrganization', this.currentOrganizationId);
        console.log('Left organization', this.currentOrganizationId);
      }
      
      await this.connection.stop();
      console.log('SignalR connection closed');
      
      this.isConnected = false;
      this.currentOrganizationId = undefined;
    } catch (err) {
      console.error('Error disconnecting from SignalR:', err);
    }
  }

  /**
   * Switch to a different organization.
   * @param organizationId New organization ID
   */
  async switchOrganization(organizationId: string): Promise<void> {
    if (this.currentOrganizationId === organizationId) {
      return;
    }

    if (this.currentOrganizationId) {
      await this.connection.invoke('LeaveOrganization', this.currentOrganizationId);
      console.log('Left organization', this.currentOrganizationId);
    }

    await this.connection.invoke('JoinOrganization', organizationId);
    console.log('Joined organization', organizationId);
    
    this.currentOrganizationId = organizationId;
  }

  /**
   * Update event handlers.
   * @param handlers New event handlers
   */
  updateHandlers(handlers: RealtimeEventHandlers): void {
    this.handlers = { ...this.handlers, ...handlers };
  }

  /**
   * Get connection state.
   */
  getConnectionState(): signalR.HubConnectionState {
    return this.connection.state;
  }

  /**
   * Check if connected.
   */
  isConnectionActive(): boolean {
    return this.isConnected && this.connection.state === signalR.HubConnectionState.Connected;
  }

  /**
   * Setup event handlers for SignalR messages.
   */
  private setupEventHandlers(): void {
    this.connection.on('ProviderHealthChanged', (update: ProviderHealthUpdate) => {
      console.log('ProviderHealthChanged:', update);
      this.handlers.onProviderHealthChanged?.(update);
    });

    this.connection.on('CostOptimizationApplied', (result: CostOptimizationResult) => {
      console.log('CostOptimizationApplied:', result);
      this.handlers.onCostOptimizationApplied?.(result);
    });

    this.connection.on('ModelDiscovered', (result: ModelDiscoveryResult) => {
      console.log('ModelDiscovered:', result);
      this.handlers.onModelDiscovered?.(result);
    });

    this.connection.on('SecurityAlert', (alert: SecurityAlert) => {
      console.log('SecurityAlert:', alert);
      this.handlers.onSecurityAlert?.(alert);
    });

    this.connection.on('AuditEvent', (event: AuditEvent) => {
      console.log('AuditEvent:', event);
      this.handlers.onAuditEvent?.(event);
    });
  }

  /**
   * Setup connection lifecycle handlers.
   */
  private setupConnectionHandlers(): void {
    this.connection.onreconnecting((error) => {
      console.warn('SignalR reconnecting...', error);
      this.isConnected = false;
    });

    this.connection.onreconnected((connectionId) => {
      console.log('SignalR reconnected', connectionId);
      this.isConnected = true;
      
      // Rejoin organization after reconnection
      if (this.currentOrganizationId) {
        this.connection.invoke('JoinOrganization', this.currentOrganizationId)
          .then(() => console.log('Rejoined organization after reconnection'))
          .catch((err) => console.error('Error rejoining organization:', err));
      }
    });

    this.connection.onclose((error) => {
      console.error('SignalR connection closed', error);
      this.isConnected = false;
    });
  }
}
