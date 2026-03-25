const API_BASE = import.meta.env.VITE_API_URL ?? '';

export interface AssignmentDto {
  id: string;
  title: string;
  type: 'Algorithm' | 'Endpoint' | 'Coverage';
  coverageThreshold: number | null;
  templateRepoUrl: string | null;
}

export interface SubmissionDto {
  id: string;
  studentId: string;
  assignmentId: string;
  gitHubUrl: string;
  branch: string;
  status: string;
  createdAt: string;
  passedTests: number | null;
  totalTests: number | null;
}

export interface SubmissionReportDto {
  submissionId: string;
  status: string;
  totalTests: number;
  passedTests: number;
  failedTests: number;
  groups: TestGroupResultDto[];
}

export interface TestGroupResultDto {
  groupName: string;
  passed: number;
  failed: number;
  errorType: string | null;
  errorMessage: string | null;
}

export interface AuthUser {
  id: string;
  login: string;
  role: string;
  profileUrl: string | null;
  hasGitHubToken: boolean;
}

export interface GitHubRepo {
  name: string;
  fullName: string;
  htmlUrl: string;
  isPrivate: boolean;
  defaultBranch: string;
}

export interface GitHubBranch {
  name: string;
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    credentials: 'include',
    ...init,
  });
  if (!res.ok) {
    const body = await res.json().catch(() => ({}));
    throw new Error(body.error ?? `HTTP ${res.status}`);
  }
  return res.json();
}

export const api = {
  // Auth
  me: () => request<AuthUser>('/api/auth/me'),
  logout: () => request<{ message: string }>('/api/auth/logout', { method: 'POST' }),

  // GitHub proxy
  getGitHubRepos: () => request<GitHubRepo[]>('/api/auth/github/repos'),
  getGitHubBranches: (owner: string, repo: string) =>
    request<GitHubBranch[]>(`/api/auth/github/repos/${owner}/${repo}/branches`),

  // Assignments
  getAssignments: () => request<AssignmentDto[]>('/api/assignments'),
  getAssignment: (id: string) => request<AssignmentDto>(`/api/assignments/${id}`),
  createAssignment: (data: {
    title: string;
    gitLabProjectId: number;
    type: 'Algorithm' | 'Endpoint' | 'Coverage';
    coverageThreshold?: number | null;
    templateRepoUrl?: string | null;
  }) =>
    request<{ id: string; link: string }>('/api/assignments', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    }),

  // Submissions
  createSubmission: (data: { assignmentId: string; gitHubUrl: string; branch: string }) =>
    request<{ id: string }>('/api/submissions', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    }),
  getSubmission: (id: string) => request<SubmissionDto>(`/api/submissions/${id}`),
  getSubmissionReport: (id: string) => request<SubmissionReportDto>(`/api/submissions/${id}/report`),
};


