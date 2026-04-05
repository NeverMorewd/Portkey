import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface ServiceEntry {
  id: number;
  name: string;
  address: string;
  port: number;
  startCommand: string;
  status: 'Stopped' | 'Running' | 'Unhealthy';
}

@Injectable({
  providedIn: 'root',
})

export class ServiceManager {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getServices(): Observable<ServiceEntry[]> {
    return this.http.get<ServiceEntry[]>(`${this.baseUrl}/api/services`);
  }

  addService(service: ServiceEntry) {
    return this.http.post<ServiceEntry>(`${this.baseUrl}/api/services`, service);
  }

  deleteService(id: number): Observable<boolean> {
    return this.http.delete<boolean>(`${this.baseUrl}/api/services/${id}`);
  }

  startService(id: number): Observable<boolean> {
    return this.http.put<boolean>(`${this.baseUrl}/api/services/${id}/start`, null);
  }

  stopService(id: number): Observable<boolean> {
    return this.http.put<boolean>(`${this.baseUrl}/api/services/${id}/stop`, null);
  }

  checkHealth(id: number): Observable<{ healthy: boolean; reason: string; checkedAt: string }> {
    return this.http.post<{ healthy: boolean; reason: string; checkedAt: string }>(
      `${this.baseUrl}/api/services/${id}/check`, null);
  }
}
