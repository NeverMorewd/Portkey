import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface PortInfo {
  port: number;
  pid: number;
  processName: string;
  protocol: string;
}

@Injectable({
  providedIn: 'root',
})
export class PortService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getPorts(): Observable<PortInfo[]> {
    return this.http.get<PortInfo[]>(`${this.baseUrl}/api/ports`);
  }
}