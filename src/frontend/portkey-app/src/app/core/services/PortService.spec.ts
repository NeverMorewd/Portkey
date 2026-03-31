import { TestBed } from '@angular/core/testing';

import { PortService } from './PortService';

describe('PortService', () => {
  let service: PortService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(PortService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
