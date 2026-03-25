import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api, type MySubmissionDto } from '../api';

const STATUS_LABELS: Record<string, string> = {
  Pending:   'Ожидание',
  Triggered: 'Запущен',
  Running:   'Выполняется',
  Passed:    'Пройдено',
  Failed:    'Не пройдено',
  Error:     'Ошибка',
  Timeout:   'Тайм-аут',
};

export default function RecentSubmissionsSidebar() {
  const [items, setItems] = useState<MySubmissionDto[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.getMyRecentSubmissions(5)
      .then(setItems)
      .catch(() => setItems([]))
      .finally(() => setLoading(false));
  }, []);

  // Don't render sidebar if user is not logged in or has no submissions
  if (loading) {
    return (
      <aside className="sidebar">
        <h3 className="sidebar__title">Последние проверки</h3>
        <p className="sidebar__empty">Загрузка…</p>
      </aside>
    );
  }

  if (items.length === 0) return null;

  return (
    <aside className="sidebar">
      <h3 className="sidebar__title">Последние проверки</h3>
      <ul className="sidebar__list">
        {items.map((s) => {
          const cls = s.status.toLowerCase();
          return (
            <li key={s.id}>
              <Link to={`/submissions/${s.id}`} className={`sidebar__item sidebar__item--${cls}`}>
                <span className={`status-dot status-dot--${cls}`} />
                <div className="sidebar__info">
                  <span className="sidebar__assignment">{s.assignmentTitle}</span>
                  <span className="sidebar__meta">
                    {s.totalTests != null
                      ? `${s.passedTests ?? 0}/${s.totalTests}`
                      : STATUS_LABELS[s.status] ?? s.status}
                    {' · '}
                    {new Date(s.createdAt).toLocaleDateString('ru-RU', {
                      day: 'numeric',
                      month: 'short',
                      hour: '2-digit',
                      minute: '2-digit',
                    })}
                  </span>
                </div>
              </Link>
            </li>
          );
        })}
      </ul>
      <Link to="/my-submissions" className="sidebar__all-link">
        Все попытки →
      </Link>
    </aside>
  );
}



