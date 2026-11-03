import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MakeReservationComponent } from './make-reservation.component';

describe('MakeReservationComponent', () => {
  let component: MakeReservationComponent;
  let fixture: ComponentFixture<MakeReservationComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MakeReservationComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(MakeReservationComponent);
    component = fixture.componentInstance;
    component.locationName = '48 Rustaveli Ave';
    component.table = { id: 'table-1', tableNumber: 1, locationId: 'location-1', capacity: 4, timeRanges: [{ start: '2030-01-01T12:00:00Z', end: '2030-01-01T14:00:00Z' }] };
    component.selectedTimeRange = { start: '2030-01-01T12:00:00Z', end: '2030-01-01T14:00:00Z' };
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
