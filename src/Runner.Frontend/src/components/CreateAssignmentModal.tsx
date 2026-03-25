import { useState } from 'react';
import { api } from '../api';

interface Props {
  open: boolean;
  onClose: () => void;
  onCreated: (id: string, link: string) => void;
}

const TYPES = [
  { value: 'Algorithm' as const, label: 'Алгоритм' },
  { value: 'Endpoint' as const, label: 'REST API' },
  { value: 'Coverage' as const, label: 'Покрытие тестами' },
];

export default function CreateAssignmentModal({ open, onClose, onCreated }: Props) {
  const [title, setTitle] = useState('');
  const [gitLabProjectId, setGitLabProjectId] = useState('');
  const [type, setType] = useState<'Algorithm' | 'Endpoint' | 'Coverage'>('Algorithm');
  const [coverageThreshold, setCoverageThreshold] = useState('');
  const [templateRepoUrl, setTemplateRepoUrl] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [result, setResult] = useState<{ id: string; link: string } | null>(null);

  if (!open) return null;

  const reset = () => {
    setTitle('');
    setGitLabProjectId('');
    setType('Algorithm');
    setCoverageThreshold('');
    setTemplateRepoUrl('');
    setError('');
    setResult(null);
  };

  const handleClose = () => {
    reset();
    onClose();
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setError('');
    try {
      const res = await api.createAssignment({
        title,
        gitLabProjectId: Number(gitLabProjectId),
        type,
        coverageThreshold: type === 'Coverage' && coverageThreshold ? Number(coverageThreshold) : null,
        templateRepoUrl: templateRepoUrl || null,
      });
      setResult(res);
      onCreated(res.id, res.link);
    } catch (err: any) {
      setError(err.message);
    } finally {
      setSubmitting(false);
    }
  };

  const fullLink = `${window.location.origin}${result?.link}`;

  return (
    <div className="modal-overlay" onClick={handleClose}>
      <div className="modal" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>Создать задание</h2>
          <button className="modal-close" onClick={handleClose}>✕</button>
        </div>

        {result ? (
          <div className="modal-body">
            <p className="success-msg">✅ Задание создано!</p>
            <label>
              <span>Ссылка на задание</span>
              <div className="copy-field">
                <input type="text" readOnly value={fullLink} />
                <button
                  type="button"
                  className="copy-btn"
                  onClick={() => navigator.clipboard.writeText(fullLink)}
                >
                  📋
                </button>
              </div>
            </label>
            <button className="btn-primary" onClick={handleClose}>Закрыть</button>
          </div>
        ) : (
          <form onSubmit={handleSubmit} className="modal-body">
            <label>
              <span>Название задания</span>
              <input
                type="text"
                placeholder="Сортировка массива"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                required
              />
            </label>

            <label>
              <span>Ссылка на шаблонный репозиторий</span>
              <input
                type="url"
                placeholder="https://github.com/org/template-repo"
                value={templateRepoUrl}
                onChange={(e) => setTemplateRepoUrl(e.target.value)}
              />
            </label>

            <label>
              <span>ID проекта GitLab (для пайплайна)</span>
              <input
                type="number"
                placeholder="12345"
                value={gitLabProjectId}
                onChange={(e) => setGitLabProjectId(e.target.value)}
                required
                min={1}
              />
            </label>

            <label>
              <span>Тип задания</span>
              <select value={type} onChange={(e) => setType(e.target.value as any)}>
                {TYPES.map((t) => (
                  <option key={t.value} value={t.value}>{t.label}</option>
                ))}
              </select>
            </label>

            {type === 'Coverage' && (
              <label>
                <span>Порог покрытия (%)</span>
                <input
                  type="number"
                  placeholder="80"
                  value={coverageThreshold}
                  onChange={(e) => setCoverageThreshold(e.target.value)}
                  min={0}
                  max={100}
                />
              </label>
            )}

            {error && <p className="error">{error}</p>}

            <button type="submit" className="btn-primary" disabled={submitting}>
              {submitting ? 'Создание…' : 'Создать задание'}
            </button>
          </form>
        )}
      </div>
    </div>
  );
}

