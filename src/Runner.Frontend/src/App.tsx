import { useEffect, useState } from 'react';
import { BrowserRouter, Routes, Route, Link } from 'react-router-dom';
import AssignmentsPage from './pages/AssignmentsPage';
import SubmitPage from './pages/SubmitPage';
import SubmissionPage from './pages/SubmissionPage';
import AuthCallbackPage from './pages/AuthCallbackPage';
import MySubmissionsPage from './pages/MySubmissionsPage';
import RecentSubmissionsSidebar from './components/RecentSubmissionsSidebar';
import { api, type AuthUser } from './api';
import './App.css';

const API_BASE = import.meta.env.VITE_API_URL ?? '';
const IS_DEV = true;

export default function App() {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [loading, setLoading] = useState(true);

  const refreshUser = () => {
    api.me().then(setUser).catch(() => setUser(null));
  };

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

  const handleDevLogin = async (role: 'student' | 'admin') => {
    try {
      await fetch(`${API_BASE}/api/auth/dev-login/${role}`, { credentials: 'include' });
      refreshUser();
    } catch { /* ignore */ }
  };

  return (
    <BrowserRouter>
      <header className="app-header">
        <div className="header-left">
          <a href="/" className="logo">Runner</a>
          {user && (
            <nav className="header-nav">
              <Link to="/">Задания</Link>
              <Link to="/my-submissions">Мои попытки</Link>
            </nav>
          )}
        </div>
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
              {IS_DEV && (
                <>
                  <button className="btn-header btn-header--dev" onClick={() => handleDevLogin('student')}>
                    Dev Student
                  </button>
                  <button className="btn-header btn-header--dev" onClick={() => handleDevLogin('admin')}>
                    Dev Admin
                  </button>
                </>
              )}
            </>
          ) : (
            <>
              <a
                href={`${API_BASE}/api/auth/login?returnUrl=/auth/callback`}
                className="btn-header btn-header--login"
              >
                Войти через GitHub
              </a>
              {IS_DEV && (
                <>
                  <button className="btn-header btn-header--dev" onClick={() => handleDevLogin('student')}>
                    Dev Student
                  </button>
                  <button className="btn-header btn-header--dev" onClick={() => handleDevLogin('admin')}>
                    Dev Admin
                  </button>
                </>
              )}
            </>
          )}
        </div>
      </header>
      <div className="app-layout">
        <main className="app-main">
          <Routes>
            <Route path="/" element={<AssignmentsPage />} />
            <Route path="/assignments/:id" element={<SubmitPage />} />
            <Route path="/submissions/:id" element={<SubmissionPage />} />
            <Route path="/my-submissions" element={<MySubmissionsPage />} />
            <Route path="/auth/callback" element={<AuthCallbackPage />} />
          </Routes>
        </main>
        {user && <RecentSubmissionsSidebar />}
      </div>
    </BrowserRouter>
  );
}
