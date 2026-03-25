import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { api, type SubmissionDto, type SubmissionReportDto } from '../api';

const STATUS_LABELS: Record<string, { label: string; emoji: string }> = {
  Pending:    { label: 'Ожидание',    emoji: '⏳' },
  Triggered:  { label: 'Запущен',     emoji: '🚀' },
  Running:    { label: 'Выполняется', emoji: '⚙️' },
  Passed:     { label: 'Пройдено',    emoji: '✅' },
  Failed:     { label: 'Не пройдено', emoji: '❌' },
  Error:      { label: 'Ошибка',      emoji: '💥' },
  Timeout:    { label: 'Тайм-аут',    emoji: '⏰' },
};

const ERROR_TYPE_LABELS: Record<string, string> = {
  CompilationError: 'Ошибка компиляции',
  AssertionFailed: 'Ошибка утверждения',
  Timeout: 'Тайм-аут',
  CoverageBelow: 'Недостаточное покрытие',
  InterfaceNotFound: 'Нет реализации интерфейса',
};

export default function SubmissionPage() {
  const { id } = useParams<{ id: string }>();

  const [submission, setSubmission] = useState<SubmissionDto | null>(null);
  const [report, setReport] = useState<SubmissionReportDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const isTerminal = (s: string) =>
    ['Passed', 'Failed', 'Error', 'Timeout'].includes(s);

  useEffect(() => {
    if (!id) return;

    const load = async () => {
      try {
        const sub = await api.getSubmission(id);
        setSubmission(sub);

        if (isTerminal(sub.status)) {
          try {
            const r = await api.getSubmissionReport(id);
            setReport(r);
          } catch { /* report may not exist yet */ }
        }
      } catch (e: any) {
        setError(e.message);
      } finally {
        setLoading(false);
      }
    };

    load();

    // Poll while not terminal
    const interval = setInterval(async () => {
      try {
        const sub = await api.getSubmission(id);
        setSubmission(sub);
        if (isTerminal(sub.status)) {
          clearInterval(interval);
          try {
            const r = await api.getSubmissionReport(id);
            setReport(r);
          } catch { /* ignore */ }
        }
      } catch { /* ignore polling errors */ }
    }, 3000);

    return () => clearInterval(interval);
  }, [id]);

  if (loading) return <p className="loading">Загрузка…</p>;
  if (error) return <p className="error">Ошибка: {error}</p>;
  if (!submission) return <p className="error">Отправка не найдена.</p>;

  const status = STATUS_LABELS[submission.status] ?? { label: submission.status, emoji: '❓' };

  return (
    <div className="page">
      <Link to="/" className="back-link">← Все задания</Link>

      <h1>Результат проверки</h1>

      <div className="status-card">
        <span className="status-emoji">{status.emoji}</span>
        <div>
          <h2 className={`status-text status--${submission.status.toLowerCase()}`}>
            {status.label}
          </h2>
          {submission.totalTests != null && (
            <p className="tests-summary">
              Тесты: {submission.passedTests ?? 0} / {submission.totalTests}
            </p>
          )}
        </div>
      </div>

      <div className="submission-details">
        <p><strong>Репозиторий:</strong>{' '}
          <a href={submission.gitHubUrl} target="_blank" rel="noopener noreferrer">
            {submission.gitHubUrl}
          </a>
        </p>
        <p><strong>Ветка:</strong> {submission.branch}</p>
        <p><strong>Дата:</strong> {new Date(submission.createdAt).toLocaleString('ru-RU')}</p>
      </div>

      {!isTerminal(submission.status) && (
        <p className="polling-hint">🔄 Автоматическое обновление каждые 3 секунды…</p>
      )}

      {report && report.groups.length > 0 && (
        <div className="report">
          <h2>Детализация по группам тестов</h2>
          <table>
            <thead>
              <tr>
                <th>Группа</th>
                <th>Пройдено</th>
                <th>Провалено</th>
                <th>Тип ошибки</th>
                <th>Сообщение</th>
              </tr>
            </thead>
            <tbody>
              {report.groups.map((g, i) => (
                <tr key={i} className={g.failed > 0 ? 'row--failed' : 'row--passed'}>
                  <td>{g.groupName}</td>
                  <td>{g.passed}</td>
                  <td>{g.failed}</td>
                  <td>{g.errorType ? (ERROR_TYPE_LABELS[g.errorType] ?? g.errorType) : '—'}</td>
                  <td className="error-msg">{g.errorMessage ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

