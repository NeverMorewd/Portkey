import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ServiceEntry, ServiceManager } from './Servicemanager';

const mockService: ServiceEntry = {
  id: 1, name: 'My API', address: 'localhost',
  port: 8080, startCommand: 'dotnet run', status: 'Stopped'
};

describe('ServiceManager', () => {
  let service: ServiceManager;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(ServiceManager);
    http    = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getServices() calls GET /api/services', () => {
    let result: ServiceEntry[] | undefined;
    service.getServices().subscribe(d => (result = d));
    const req = http.expectOne(r => r.url.includes('/api/services'));
    expect(req.request.method).toBe('GET');
    req.flush([mockService]);
    expect(result).toHaveLength(1);
    expect(result![0].name).toBe('My API');
  });

  it('addService() calls POST /api/services with body', () => {
    service.addService(mockService).subscribe();
    const req = http.expectOne(r => r.url.includes('/api/services') && r.method === 'POST');
    expect(req.request.body).toMatchObject({ name: 'My API' });
    req.flush(mockService);
  });

  it('startService() calls PUT /api/services/{id}/start', () => {
    service.startService(1).subscribe();
    const req = http.expectOne(r => r.url.includes('/api/services/1/start'));
    expect(req.request.method).toBe('PUT');
    req.flush(true);
  });

  it('stopService() calls PUT /api/services/{id}/stop', () => {
    service.stopService(1).subscribe();
    const req = http.expectOne(r => r.url.includes('/api/services/1/stop'));
    expect(req.request.method).toBe('PUT');
    req.flush(true);
  });

  it('deleteService() calls DELETE /api/services/{id}', () => {
    service.deleteService(1).subscribe();
    const req = http.expectOne(r => r.url.includes('/api/services/1') && r.method === 'DELETE');
    req.flush(true);
  });

  it('checkHealth() calls POST /api/services/{id}/check', () => {
    let result: { healthy: boolean; reason: string; checkedAt: string } | undefined;
    service.checkHealth(1).subscribe(r => (result = r));
    const req = http.expectOne(r => r.url.includes('/api/services/1/check'));
    expect(req.request.method).toBe('POST');
    req.flush({ healthy: true, reason: 'TCP connection succeeded', checkedAt: new Date().toISOString() });
    expect(result?.healthy).toBe(true);
  });
});
