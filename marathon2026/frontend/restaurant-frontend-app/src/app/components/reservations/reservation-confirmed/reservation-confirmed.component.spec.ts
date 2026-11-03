import { Reservation } from '@app/models/reservations/reservation.model';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ReservationConfirmedComponent } from './reservation-confirmed.component';

describe('ReservationConfirmedComponent', () => {
  let component: ReservationConfirmedComponent;
  let fixture: ComponentFixture<ReservationConfirmedComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ReservationConfirmedComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ReservationConfirmedComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('reservation', {
      id: 'reservation-1',
      tableId: 'table-1',
      customerId: 'customer-1',
      startTime: new Date('2030-01-01T12:00:00Z'),
      endTime: new Date('2030-01-01T14:00:00Z'),
      guestCount: 2,
      locationAddress: '48 Rustaveli Ave',
      tableCapacity: 4,
      waiterId: 'waiter-1',
    } as unknown as Reservation);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
