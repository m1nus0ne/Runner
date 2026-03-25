import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../api';

export default function AuthCallbackPage() {
  const navigate = useNavigate();
  const [error, setError] = useState('');

  useEffect(() => {
    api.me()
      .then(() => {
        // Авторизация прошла — переходим на главную
        navigate('/', { replace: true });
      })
      .catch(() => {
        setError('Не удалось авторизоваться. Попробуйте ещё раз.');
      });
  }, [navigate]);

  if (error) {
    return (
      <div className="page" style={{ textAlign: 'center', paddingTop: '4rem' }}>
        <p className="error">{error}</p>
        <a href="/" className="back-link">← На главную</a>
      </div>
    );
  }

  return (
    <div className="page" style={{ textAlign: 'center', paddingTop: '4rem' }}>
      <p className="loading">🔐 Авторизация…</p>
    </div>
  );
}

