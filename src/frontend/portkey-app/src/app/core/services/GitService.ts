import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface GitRoot {
  id: number;
  path: string;
}

export interface GitRepoInfo {
  path: string;
  name: string;
  branch: string;
  isDirty: boolean;
  changedFiles: number;
  untrackedFiles: number;
  lastCommitMessage: string;
  lastCommitAuthor: string;
  lastCommitDate: string | null;
  aheadBy: number | null;
  behindBy: number | null;
  remoteUrl: string | null;
}

@Injectable({ providedIn: 'root' })
export class GitService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/git`;

  getRoots(): Observable<GitRoot[]> {
    return this.http.get<GitRoot[]>(`${this.base}/roots`);
  }

  addRoot(path: string): Observable<GitRoot> {
    return this.http.post<GitRoot>(`${this.base}/roots`, { path });
  }

  deleteRoot(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/roots/${id}`);
  }

  getRepos(): Observable<GitRepoInfo[]> {
    return this.http.get<GitRepoInfo[]>(`${this.base}/repos`);
  }

  refreshRepo(path: string): Observable<GitRepoInfo> {
    return this.http.get<GitRepoInfo>(`${this.base}/repos/refresh`, { params: { path } });
  }
}
