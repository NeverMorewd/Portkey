import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PortList } from './port-list';

describe('PortList', () => {
  let component: PortList;
  let fixture: ComponentFixture<PortList>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PortList],
    }).compileComponents();

    fixture = TestBed.createComponent(PortList);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
