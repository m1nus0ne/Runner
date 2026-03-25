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

export default function MySubmissionsPage() {
  const [submissions, setSubmissions] = useState<MySubmissionDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    api.getMySubmissions()
      .then(setSubmissions)
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <p className="loading">Загрузка попыток…</p>;
  if (error) return <p className="error">Ошибка: {error}</p>;

  // group by assignment
  const grouped = submissions.reduce<Record<string, MySubmissionDto[]>>((acc, s) => {
    if (!acc[s.assignmentId]) acc[s.assignmentId] = [];
    acc[s.assignmentId].push(s);
    return acc;
  }, {});

  return (
    <div className="page">
      <Link to="/" className="back-link">← Все задания</Link>
      <h1>Мои попытки</h1>

      {submissions.length === 0 ? (
        <p className="hint">У вас пока нет отправленных решений.</p>
      ) : (
        Object.entries(grouped).map(([assignmentId, subs]) => (
          <div key={assignmentId} className="submissions-group">
            <h2 className="submissions-group__title">
              <Link to={`/assignments/${assignmentId}`}>{subs[0].assignmentTitle}</Link>
            </h2>
            <div className="submissions-table-wrap">
              <table className="submissions-table">
                <thead>
                  <tr>
                    <th>Статус</th>
                    <th>Тесты</th>
                    <th>Ветка</th>
                    <th>Дата</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  {subs.map((s) => {
                    const st = STATUS_LABELS[s.status] ?? s.status;
                    return (
                      <tr key={s.id} className={`sub-row sub-row--${s.status.toLowerCase()}`}>
                        <td>
                          <span className="sub-status">
                            <span className={`status-dot status-dot--${s.status.toLowerCase()}`} />
                            {st}
                          </span>
                        </td>
                        <td>
                          {s.totalTests != null
                            ? `${s.passedTests ?? 0} / ${s.totalTests}`
                            : '—'}
                        </td>
                        <td><code>{s.branch}</code></td>
                        <td>{new Date(s.createdAt).toLocaleString('ru-RU')}</td>
                        <td>
                          <Link to={`/submissions/${s.id}`} className="btn-sm">
                            Подробнее →
                          </Link>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          </div>
        ))
      )}
    </div>
  );
}



