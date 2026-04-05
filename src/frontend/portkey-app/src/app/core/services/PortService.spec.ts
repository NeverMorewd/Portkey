import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { PortInfo, PortService } from './PortService';

describe('PortService', () => {
  let service: PortService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(PortService);
    http    = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getPorts() calls GET /api/ports and returns data', () => {
    const mock: PortInfo[] = [
      { port: 3000, pid: 1234, processName: 'node', protocol: 'TCP' },
      { port: 5432, pid: 5678, processName: 'postgres', protocol: 'TCP' },
    ];

    let result: PortInfo[] | undefined;
    service.getPorts().subscribe(d => (result = d));

    const req = http.expectOne(r => r.url.includes('/api/ports'));
    expect(req.request.method).toBe('GET');
    req.flush(mock);

    expect(result).toEqual(mock);
  });

  it('getPorts() propagates HTTP errors', () => {
    let errored = false;
    service.getPorts().subscribe({ error: () => (errored = true) });
    http.expectOne(r => r.url.includes('/api/ports')).flush('Server Error', {
      status: 500, statusText: 'Internal Server Error'
    });
    expect(errored).toBe(true);
  });
});
