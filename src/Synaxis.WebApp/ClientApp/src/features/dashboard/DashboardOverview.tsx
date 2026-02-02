export default function DashboardOverview() {
  return (
    <div className="space-y-6">
      <h2 className="text-2xl font-bold">Dashboard Overview</h2>
      <p className="text-[var(--muted-foreground)]">
        Welcome to the Synaxis Dashboard. Use the sidebar to navigate to different sections.
      </p>
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        <div className="p-4 border border-[var(--border)] rounded-lg">
          <h3 className="font-semibold mb-2">Providers</h3>
          <p className="text-sm text-[var(--muted-foreground)]">Manage AI providers and their configurations</p>
        </div>
        <div className="p-4 border border-[var(--border)] rounded-lg">
          <h3 className="font-semibold mb-2">Analytics</h3>
          <p className="text-sm text-[var(--muted-foreground)]">View usage statistics and performance metrics</p>
        </div>
        <div className="p-4 border border-[var(--border)] rounded-lg">
          <h3 className="font-semibold mb-2">API Keys</h3>
          <p className="text-sm text-[var(--muted-foreground)]">Manage your API keys and access tokens</p>
        </div>
      </div>
    </div>
  )
}
