import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import AdminShell from './AdminShell';
import AdminLogin from './AdminLogin';

const mockNavigate = vi.fn();
const mockSetJwtToken = vi.fn();
const mockLogout = vi.fn();

let mockJwtToken: string | null = null;

vi.mock('@/stores/settings', () => ({
  default: (selector: (s: { jwtToken: string | null; setJwtToken: () => void; logout: () => void }) => unknown) => selector({
    jwtToken: mockJwtToken,
    setJwtToken: mockSetJwtToken,
    logout: mockLogout,
  }),
}));

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

describe('AdminShell', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockJwtToken = 'valid-jwt-token';
  });

  it('should render admin shell with navigation', () => {
    render(
      <MemoryRouter initialEntries={['/admin']}>
        <Routes>
          <Route path="/admin/*" element={<AdminShell />}>
            <Route index element={<div>Dashboard Content</div>} />
          </Route>
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByText('Synaxis')).toBeInTheDocument();
    expect(screen.getByText('Admin Panel')).toBeInTheDocument();
    expect(screen.getByText('Health Dashboard')).toBeInTheDocument();
    expect(screen.getByText('Provider Config')).toBeInTheDocument();
    expect(screen.getByText('Settings')).toBeInTheDocument();
  });

  it('should show logout button', () => {
    render(
      <MemoryRouter initialEntries={['/admin']}>
        <Routes>
          <Route path="/admin/*" element={<AdminShell />}>
            <Route index element={<div>Dashboard Content</div>} />
          </Route>
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByText('Logout')).toBeInTheDocument();
  });

  it('should call logout and navigate on logout click', () => {
    render(
      <MemoryRouter initialEntries={['/admin']}>
        <Routes>
          <Route path="/admin/*" element={<AdminShell />}>
            <Route index element={<div>Dashboard Content</div>} />
          </Route>
        </Routes>
      </MemoryRouter>
    );

    fireEvent.click(screen.getByText('Logout'));

    expect(mockLogout).toHaveBeenCalled();
    expect(mockNavigate).toHaveBeenCalledWith('/admin/login');
  });

  it('should highlight active navigation item', () => {
    render(
      <MemoryRouter initialEntries={['/admin/health']}>
        <Routes>
          <Route path="/admin/*" element={<AdminShell />}>
            <Route path="health" element={<div>Health Content</div>} />
          </Route>
        </Routes>
      </MemoryRouter>
    );

    const healthLink = screen.getByText('Health Dashboard').closest('a');
    expect(healthLink).toHaveClass('bg-[var(--primary)]/10');
  });
});

describe('AdminLogin', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should render login form', () => {
    render(
      <MemoryRouter>
        <AdminLogin />
      </MemoryRouter>
    );

    expect(screen.getByText('Synaxis Admin')).toBeInTheDocument();
    expect(screen.getByLabelText('JWT Token')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Access Admin Panel' })).toBeInTheDocument();
  });

  it('should show error for empty token', () => {
    render(
      <MemoryRouter>
        <AdminLogin />
      </MemoryRouter>
    );

    fireEvent.click(screen.getByRole('button', { name: 'Access Admin Panel' }));

    expect(screen.getByText('Please enter a JWT token')).toBeInTheDocument();
  });

  it('should show error for invalid JWT format', () => {
    render(
      <MemoryRouter>
        <AdminLogin />
      </MemoryRouter>
    );

    const input = screen.getByLabelText('JWT Token');
    fireEvent.change(input, { target: { value: 'invalid-token' } });
    fireEvent.click(screen.getByRole('button', { name: 'Access Admin Panel' }));

    expect(screen.getByText('Invalid JWT token format')).toBeInTheDocument();
  });

  it('should navigate to admin on valid JWT', () => {
    const validToken = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwiZW1haWwiOiJhZG1pbkBleGFtcGxlLmNvbSJ9.signature';

    render(
      <MemoryRouter>
        <AdminLogin />
      </MemoryRouter>
    );

    const input = screen.getByLabelText('JWT Token');
    fireEvent.change(input, { target: { value: validToken } });
    fireEvent.click(screen.getByRole('button', { name: 'Access Admin Panel' }));

    expect(mockSetJwtToken).toHaveBeenCalledWith(validToken);
    expect(mockNavigate).toHaveBeenCalledWith('/admin');
  });

  it('should have link back to chat', () => {
    render(
      <MemoryRouter>
        <AdminLogin />
      </MemoryRouter>
    );

    expect(screen.getByText('Back to Chat')).toBeInTheDocument();
  });

  it('should toggle token visibility', () => {
    render(
      <MemoryRouter>
        <AdminLogin />
      </MemoryRouter>
    );

    const input = screen.getByLabelText('JWT Token');
    const toggleButton = screen.getByLabelText('Show token');

    expect(input).toHaveAttribute('type', 'password');

    fireEvent.click(toggleButton);

    expect(input).toHaveAttribute('type', 'text');
    expect(screen.getByLabelText('Hide token')).toBeInTheDocument();
  });
});
