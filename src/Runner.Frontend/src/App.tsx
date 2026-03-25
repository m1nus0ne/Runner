import { useEffect, useState } from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import AssignmentsPage from './pages/AssignmentsPage';
import SubmitPage from './pages/SubmitPage';
import SubmissionPage from './pages/SubmissionPage';
import AuthCallbackPage from './pages/AuthCallbackPage';
import { api, type AuthUser } from './api';
import './App.css';

const API_BASE = import.meta.env.VITE_API_URL ?? '';

export default function App() {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.me()
      .then(setUser)
      .catch(() => setUser(null))
      .finally(() => setLoading(false));
  }, []);

  const handleLogout = async () => {
    try {
      await api.logout();
    } catch { /* ignore */ }
    setUser(null);
  };

  return (
    <BrowserRouter>
      <header className="app-header">
        <a href="/" className="logo">🏃 Runner</a>
        <div className="header-right">
          {loading ? null : user ? (
            <>
              <span className="user-info">
                {user.login}
                <span className={`role-badge role-badge--${user.role.toLowerCase()}`}>
                  {user.role}
                </span>
              </span>
              <button className="btn-header btn-header--logout" onClick={handleLogout}>
                Выйти
              </button>
            </>
          ) : (
            <a
              href={`${API_BASE}/auth/login?returnUrl=/auth/callback`}
              className="btn-header btn-header--login"
            >
              Войти через GitHub
            </a>
          )}
        </div>
      </header>
      <main>
        <Routes>
          <Route path="/" element={<AssignmentsPage />} />
          <Route path="/assignments/:id" element={<SubmitPage />} />
          <Route path="/submissions/:id" element={<SubmissionPage />} />
          <Route path="/auth/callback" element={<AuthCallbackPage />} />
        </Routes>
      </main>
    </BrowserRouter>
  );
}
