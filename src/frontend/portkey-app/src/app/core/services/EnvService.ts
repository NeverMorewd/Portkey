import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface EnvProject {
  id: number;
  name: string;
  path: string;
}

export interface EnvEntry {
  key: string;
  value: string;
  isSensitive: boolean;
  comment?: string;
}

@Injectable({ providedIn: 'root' })
export class EnvService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/env`;

  getProjects(): Observable<EnvProject[]> {
    return this.http.get<EnvProject[]>(`${this.base}/projects`);
  }

  addProject(project: Omit<EnvProject, 'id'>): Observable<EnvProject> {
    return this.http.post<EnvProject>(`${this.base}/projects`, project);
  }

  deleteProject(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/projects/${id}`);
  }

  getFiles(id: number): Observable<string[]> {
    return this.http.get<string[]>(`${this.base}/projects/${id}/files`);
  }

  getEntries(id: number, file: string): Observable<EnvEntry[]> {
    return this.http.get<EnvEntry[]>(`${this.base}/projects/${id}/file`, { params: { name: file } });
  }

  saveEntries(id: number, file: string, entries: EnvEntry[]): Observable<void> {
    return this.http.put<void>(`${this.base}/projects/${id}/file`, entries, { params: { name: file } });
  }
}
