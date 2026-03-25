import { useEffect, useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { api, type AssignmentDto } from '../api';

const TYPE_LABELS: Record<string, string> = {
  Algorithm: 'Алгоритм',
  Endpoint: 'REST API',
  Coverage: 'Покрытие тестами',
};

export default function SubmitPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [assignment, setAssignment] = useState<AssignmentDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  // form
  const [gitHubUrl, setGitHubUrl] = useState('');
  const [branch, setBranch] = useState('main');
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState('');

  useEffect(() => {
    if (!id) return;
    api.getAssignment(id)
      .then(setAssignment)
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false));
  }, [id]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!id) return;
    setSubmitting(true);
    setSubmitError('');
    try {
      const { id: submissionId } = await api.createSubmission({
        assignmentId: id,
        gitHubUrl,
        branch,
      });
      navigate(`/submissions/${submissionId}`);
    } catch (err: any) {
      setSubmitError(err.message);
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) return <p className="loading">Загрузка задания…</p>;
  if (error) return <p className="error">Ошибка: {error}</p>;
  if (!assignment) return <p className="error">Задание не найдено.</p>;

  return (
    <div className="page">
      <Link to="/" className="back-link">← Все задания</Link>

      <div className="assignment-header">
        <h1>{assignment.title}</h1>
        <span className={`badge badge--${assignment.type.toLowerCase()}`}>
          {TYPE_LABELS[assignment.type] ?? assignment.type}
        </span>
      </div>

      {assignment.coverageThreshold != null && (
        <p className="meta">
          Минимальное покрытие кода: <strong>{assignment.coverageThreshold}%</strong>
        </p>
      )}

      {assignment.templateRepoUrl && (
        <div className="template-block">
          <h3>📁 Шаблонный репозиторий</h3>
          <a
            href={assignment.templateRepoUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="template-link"
          >
            {assignment.templateRepoUrl}
          </a>
          <p className="hint">
            Создайте свой репозиторий на основе этого шаблона, выполните задание,
            затем отправьте ссылку ниже.
          </p>
        </div>
      )}

      <form onSubmit={handleSubmit} className="submit-form">
        <h2>Отправить решение</h2>

        <label>
          <span>URL GitHub-репозитория</span>
          <input
            type="url"
            placeholder="https://github.com/username/repo"
            value={gitHubUrl}
            onChange={(e) => setGitHubUrl(e.target.value)}
            required
          />
        </label>

        <label>
          <span>Ветка</span>
          <input
            type="text"
            placeholder="main"
            value={branch}
            onChange={(e) => setBranch(e.target.value)}
            required
          />
        </label>

        {submitError && <p className="error">{submitError}</p>}

        <button type="submit" disabled={submitting}>
          {submitting ? 'Отправка…' : 'Отправить на проверку'}
        </button>
      </form>
    </div>
  );
}

