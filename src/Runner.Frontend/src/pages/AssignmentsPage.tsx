import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api, type AssignmentDto, type AuthUser } from '../api';
import CreateAssignmentModal from '../components/CreateAssignmentModal';

const TYPE_LABELS: Record<string, string> = {
  Algorithm: 'Алгоритм',
  Endpoint: 'REST API',
  Coverage: 'Покрытие тестами',
};

export default function AssignmentsPage() {
  const [assignments, setAssignments] = useState<AssignmentDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [user, setUser] = useState<AuthUser | null>(null);
  const [modalOpen, setModalOpen] = useState(false);

  useEffect(() => {
    api.me().then(setUser).catch(() => {});
    api.getAssignments()
      .then(setAssignments)
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false));
  }, []);

  const isAdmin = user?.role === 'Admin';

  const handleCreated = (_id: string) => {
    setModalOpen(false);
    api.getAssignments().then(setAssignments).catch(() => {});
  };

  if (loading) return <p className="loading">Загрузка заданий…</p>;
  if (error) return <p className="error">Ошибка: {error}</p>;

  return (
    <div className="page">
      <div className="page-title-row">
        <h1>Задания</h1>
        {isAdmin && (
          <button className="btn-primary" onClick={() => setModalOpen(true)}>
            + Создать задание
          </button>
        )}
      </div>

      {assignments.length === 0 ? (
        <p>Нет доступных заданий.</p>
      ) : (
        <div className="card-grid">
          {assignments.map((a) => (
            <Link to={`/assignments/${a.id}`} key={a.id} className="card">
              <h2>{a.title}</h2>
              <span className={`badge badge--${a.type.toLowerCase()}`}>
                {TYPE_LABELS[a.type] ?? a.type}
              </span>
              {a.coverageThreshold != null && (
                <p className="meta">Порог покрытия: {a.coverageThreshold}%</p>
              )}
            </Link>
          ))}
        </div>
      )}

      <CreateAssignmentModal
        open={modalOpen}
        onClose={() => setModalOpen(false)}
        onCreated={handleCreated}
      />
    </div>
  );
}
