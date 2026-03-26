import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { api, type SubmissionDto, type SubmissionReportDto, type TestGroupResultDto, type FailedTestDetailDto } from '../api';

const STATUS_LABELS: Record<string, string> = {
  Pending:    'Ожидание',
  Triggered:  'Запущен',
  Running:    'Выполняется',
  Passed:     'Пройдено',
  Failed:     'Не пройдено',
  Error:      'Ошибка',
  Timeout:    'Тайм-аут',
};

const ERROR_TYPE_LABELS: Record<string, string> = {
  CompilationError: 'Ошибка компиляции',
  AssertionFailed: 'Ошибка утверждения',
  Timeout: 'Тайм-аут',
  CoverageBelow: 'Недостаточное покрытие',
  InterfaceNotFound: 'Нет реализации интерфейса',
};

/** Пробует распарсить строку как JSON и вернуть pretty-printed. */
function tryPrettyJson(value: string | null): { isJson: boolean; formatted: string } {
  if (!value) return { isJson: false, formatted: value ?? '' };
  const trimmed = value.trim();
  if ((trimmed.startsWith('{') && trimmed.endsWith('}')) ||
      (trimmed.startsWith('[') && trimmed.endsWith(']'))) {
    try {
      const parsed = JSON.parse(trimmed);
      return { isJson: true, formatted: JSON.stringify(parsed, null, 2) };
    } catch { /* not JSON */ }
  }
  return { isJson: false, formatted: value };
}

/** Компонент для рендера expected/actual значения — если JSON, рисуем красиво */
function ValueBlock({ label, value, className }: { label: string; value: string | null; className?: string }) {
  if (!value) return null;
  const { isJson, formatted } = tryPrettyJson(value);
  return (
    <div className={`value-block ${className ?? ''}`}>
      <span className="value-block__label">{label}</span>
      {isJson ? (
        <pre className="value-block__json">{formatted}</pre>
      ) : (
        <code className="value-block__code">{formatted}</code>
      )}
    </div>
  );
}

/** Модалка с деталями упавших тестов группы */
function FailedTestsModal({
  group,
  onClose,
}: {
  group: TestGroupResultDto;
  onClose: () => void;
}) {
  const tests = group.failedTests ?? [];

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal modal--wide" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>{group.groupName}</h2>
          <button className="modal-close" onClick={onClose}>&times;</button>
        </div>
        <div className="modal-body">
          {group.errorType && (
            <p className="ftm-error-type">
              {ERROR_TYPE_LABELS[group.errorType] ?? group.errorType}
            </p>
          )}
          <p className="ftm-summary">
            Пройдено: <strong className="text-green">{group.passed}</strong>
            {' / '}
            Провалено: <strong className="text-red">{group.failed}</strong>
          </p>

          {tests.length === 0 ? (
            <p className="hint">Нет структурированных данных об ошибках.</p>
          ) : (
            <div className="ftm-tests">
              {tests.map((t, i) => (
                <FailedTestCard key={i} test={t} />
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

function FailedTestCard({ test }: { test: FailedTestDetailDto }) {
  const hasExpectedActual = test.expected != null || test.actual != null;

  return (
    <div className="ftm-card">
      <h4 className="ftm-card__name">{test.testName}</h4>

      {hasExpectedActual ? (
        <div className="ftm-diff">
          <ValueBlock label="Ожидалось" value={test.expected} className="value-block--expected" />
          <ValueBlock label="Получено" value={test.actual} className="value-block--actual" />
        </div>
      ) : (
        <pre className="ftm-card__message">{test.message}</pre>
      )}
    </div>
  );
}

export default function SubmissionPage() {
  const { id } = useParams<{ id: string }>();

  const [submission, setSubmission] = useState<SubmissionDto | null>(null);
  const [report, setReport] = useState<SubmissionReportDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [selectedGroup, setSelectedGroup] = useState<TestGroupResultDto | null>(null);

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

  const statusLabel = STATUS_LABELS[submission.status] ?? submission.status;

  return (
    <div className="page">
      <Link to="/" className="back-link">← Все задания</Link>

      <h1>Результат проверки</h1>

      <div className="status-card">
        <div>
          <span className={`status-dot status-dot--${submission.status.toLowerCase()}`} />
        </div>
        <div>
          <h2 className={`status-text status--${submission.status.toLowerCase()}`}>
            {statusLabel}
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
        <p className="polling-hint">Автоматическое обновление каждые 3 секунды…</p>
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
                <th></th>
              </tr>
            </thead>
            <tbody>
              {report.groups.map((g, i) => {
                const hasFailed = g.failed > 0;
                return (
                  <tr
                    key={i}
                    className={`${hasFailed ? 'row--failed' : 'row--passed'} ${hasFailed ? 'row--clickable' : ''}`}
                    onClick={() => hasFailed && setSelectedGroup(g)}
                  >
                    <td>{g.groupName}</td>
                    <td>{g.passed}</td>
                    <td>{g.failed}</td>
                    <td>{g.errorType ? (ERROR_TYPE_LABELS[g.errorType] ?? g.errorType) : '—'}</td>
                    <td>
                      {hasFailed && (
                        <span className="btn-sm">Подробнее →</span>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      {selectedGroup && (
        <FailedTestsModal
          group={selectedGroup}
          onClose={() => setSelectedGroup(null)}
        />
      )}
    </div>
  );
}

