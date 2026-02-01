Fixes applied for Task 10.2 - TypeScript unused import errors

Changes:
- Removed unused `Server` import from src/Synaxis.WebApp/ClientApp/src/features/admin/HealthDashboard.tsx
- Removed unused `ChatStreamChunk` import from src/Synaxis.WebApp/ClientApp/src/features/chat/ChatWindow.tsx

Notes:
- No behavioral or logic changes were made; only imports were removed to satisfy TypeScript compiler.
- After edits, `npm run build` in ClientApp completed successfully with Vite build output and no TypeScript errors.

Additional lint diagnostics (from lsp):
- HealthDashboard.tsx: biome lint requested explicit button type props; left unchanged per task constraint (no functional changes), as these are warnings/errors unrelated to the unused-import build failures.
- ChatWindow.tsx: biome reported hook dependency and aria role concerns; also left untouched to avoid changing behavior.

If maintainers want, I can follow up to address the additional lint errors in a separate task.
