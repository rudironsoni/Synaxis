import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import useSettingsStore, { type SettingsState } from '@/stores/settings';

export function useAuth(requireAuth: boolean = true) {
  const navigate = useNavigate();
  const jwtToken = useSettingsStore((s: SettingsState) => s.jwtToken);

  useEffect(() => {
    if (requireAuth && !jwtToken) {
      navigate('/admin/login');
    }
  }, [requireAuth, jwtToken, navigate]);

  return { isAuthenticated: !!jwtToken, jwtToken };
}

export function useIsAuthenticated(): boolean {
  const jwtToken = useSettingsStore((s: SettingsState) => s.jwtToken);
  return !!jwtToken;
}

export function useLogout(): () => void {
  const logout = useSettingsStore((s: SettingsState) => s.logout);
  return logout;
}