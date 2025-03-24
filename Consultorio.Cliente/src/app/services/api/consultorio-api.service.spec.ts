import { TestBed } from '@angular/core/testing';

import { ConsultorioApiService } from './consultorio-api.service';

describe('ConsultorioApiService', () => {
  let service: ConsultorioApiService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ConsultorioApiService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
