import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import useSettingsStore from '@/stores/settings';

export function useAuth(requireAuth: boolean = true) {
  const navigate = useNavigate();
  const jwtToken = useSettingsStore((s: { jwtToken: string | undefined }) => s.jwtToken);

  useEffect(() => {
    if (requireAuth && !jwtToken) {
      navigate('/admin/login');
    }
  }, [requireAuth, jwtToken, navigate]);

  return { isAuthenticated: !!jwtToken, jwtToken };
}

export function useIsAuthenticated(): boolean {
  const jwtToken = useSettingsStore((s: { jwtToken: string | undefined }) => s.jwtToken);
  return !!jwtToken;
}

export function useLogout(): () => void {
  const logout = useSettingsStore((s: { logout: () => void }) => s.logout);
  return logout;
}