import { useEffect, useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { api, type AssignmentDto, type AuthUser, type GitHubRepo, type GitHubBranch } from '../api';

const TYPE_LABELS: Record<string, string> = {
  Algorithm: 'Алгоритм',
  Endpoint: 'REST API',
  Coverage: 'Покрытие тестами',
};

const API_BASE = import.meta.env.VITE_API_URL ?? '';

export default function SubmitPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [assignment, setAssignment] = useState<AssignmentDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  // current user
  const [user, setUser] = useState<AuthUser | null>(null);

  // repos & branches
  const [repos, setRepos] = useState<GitHubRepo[]>([]);
  const [reposLoading, setReposLoading] = useState(false);
  const [selectedRepo, setSelectedRepo] = useState<GitHubRepo | null>(null);

  const [branches, setBranches] = useState<GitHubBranch[]>([]);
  const [branchesLoading, setBranchesLoading] = useState(false);
  const [selectedBranch, setSelectedBranch] = useState('');

  // manual fallback — начинаем в manual, переключим если есть токен
  const [manualMode, setManualMode] = useState(true);
  const [manualUrl, setManualUrl] = useState('');
  const [manualBranch, setManualBranch] = useState('main');

  // submit
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState('');
  const [cloneCopied, setCloneCopied] = useState(false);

  const gitCloneCmd = assignment?.templateRepoUrl
    ? `git clone ${assignment.templateRepoUrl}`
    : '';

  const copyCloneCmd = () => {
    navigator.clipboard.writeText(gitCloneCmd);
    setCloneCopied(true);
    setTimeout(() => setCloneCopied(false), 2000);
  };

  // Load assignment
  useEffect(() => {
    if (!id) return;
    api.getAssignment(id)
      .then(setAssignment)
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false));
  }, [id]);

  // Load user, then decide mode & load repos
  useEffect(() => {
    api.me()
      .then((u) => {
        setUser(u);
        if (u.hasGitHubToken) {
          // У пользователя есть GitHub-токен → загружаем репозитории
          setManualMode(false);
          setReposLoading(true);
          api.getGitHubRepos()
            .then((r) => setRepos(r))
            .catch(() => {})
            .finally(() => setReposLoading(false));
        }
        // Если нет токена — остаёмся в manualMode
      })
      .catch(() => {});
  }, []);

  // Load branches when repo changes
  useEffect(() => {
    if (!selectedRepo) {
      setBranches([]);
      setSelectedBranch('');
      return;
    }

    const [owner, repo] = selectedRepo.fullName.split('/');
    setBranchesLoading(true);
    setBranches([]);
    setSelectedBranch('');

    api.getGitHubBranches(owner, repo)
      .then((b) => {
        setBranches(b);
        const def = b.find((br) => br.name === selectedRepo.defaultBranch);
        if (def) setSelectedBranch(def.name);
        else if (b.length > 0) setSelectedBranch(b[0].name);
      })
      .catch(() => setBranches([]))
      .finally(() => setBranchesLoading(false));
  }, [selectedRepo]);

  const handleRepoSelect = (fullName: string) => {
    const repo = repos.find((r) => r.fullName === fullName) ?? null;
    setSelectedRepo(repo);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!id) return;

    const gitHubUrl = manualMode ? manualUrl : selectedRepo?.htmlUrl ?? '';
    const branch = manualMode ? manualBranch : selectedBranch;

    if (!gitHubUrl || !branch) return;

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

  const hasToken = user?.hasGitHubToken ?? false;

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

          <div className="clone-field">
            <code>{gitCloneCmd}</code>
            <button
              type="button"
              className="copy-btn"
              onClick={copyCloneCmd}
              title="Копировать"
            >
              {cloneCopied ? '✅' : '📋'}
            </button>
          </div>

          <p className="hint">
            Создайте свой репозиторий на основе этого шаблона, выполните задание,
            затем отправьте ссылку ниже.
          </p>
        </div>
      )}

      <form onSubmit={handleSubmit} className="submit-form">
        <h2>Отправить решение</h2>

        {!manualMode ? (
          <>
            <label>
              <span>Репозиторий</span>
              {reposLoading ? (
                <select disabled><option>Загрузка репозиториев…</option></select>
              ) : (
                <select
                  value={selectedRepo?.fullName ?? ''}
                  onChange={(e) => handleRepoSelect(e.target.value)}
                  required
                >
                  <option value="" disabled>— Выберите репозиторий —</option>
                  {repos.map((r) => (
                    <option key={r.fullName} value={r.fullName}>
                      {r.fullName}
                    </option>
                  ))}
                </select>
              )}
            </label>

            <label>
              <span>Ветка</span>
              {branchesLoading ? (
                <select disabled><option>Загрузка веток…</option></select>
              ) : (
                <select
                  value={selectedBranch}
                  onChange={(e) => setSelectedBranch(e.target.value)}
                  required
                  disabled={!selectedRepo || branches.length === 0}
                >
                  {branches.length === 0 && (
                    <option value="" disabled>
                      {selectedRepo ? 'Нет веток' : '— Сначала выберите репозиторий —'}
                    </option>
                  )}
                  {branches.map((b) => (
                    <option key={b.name} value={b.name}>{b.name}</option>
                  ))}
                </select>
              )}
            </label>

            <button
              type="button"
              className="btn-link"
              onClick={() => setManualMode(true)}
            >
              Ввести URL вручную
            </button>
          </>
        ) : (
          <>
            {!hasToken && (
              <p className="hint" style={{ marginBottom: '1rem' }}>
                💡 <a href={`${API_BASE}/api/auth/login?returnUrl=${encodeURIComponent(window.location.pathname)}`}>
                  Войдите через GitHub
                </a>{' '}
                для автоматического выбора репозитория и ветки из вашего аккаунта.
              </p>
            )}

            <label>
              <span>URL GitHub-репозитория</span>
              <input
                type="url"
                placeholder="https://github.com/username/repo"
                value={manualUrl}
                onChange={(e) => setManualUrl(e.target.value)}
                required
              />
            </label>

            <label>
              <span>Ветка</span>
              <input
                type="text"
                placeholder="main"
                value={manualBranch}
                onChange={(e) => setManualBranch(e.target.value)}
                required
              />
            </label>

            {hasToken && (
              <button
                type="button"
                className="btn-link"
                onClick={() => setManualMode(false)}
              >
                Выбрать из списка репозиториев
              </button>
            )}
          </>
        )}

        {submitError && <p className="error">{submitError}</p>}

        <button type="submit" disabled={submitting}>
          {submitting ? 'Отправка…' : 'Отправить на проверку'}
        </button>
      </form>
    </div>
  );
}

