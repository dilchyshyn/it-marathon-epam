import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { CardReservationComponent } from './card-reservation.component';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';
import { ReservationCustomer, ReservationStatusCustomer } from '@app/models/reservations/reservation.model';

const RESERVATION: ReservationCustomer = {
  id: 'res-1',
  tableId: 'table-1',
  startTime: new Date('2026-10-21T16:00:00Z'),
  endTime: new Date('2026-10-21T17:30:00Z'),
  guestCount: 2,
  status: ReservationStatusCustomer.Reserved,
  restaurantName: 'Green & Tasty',
  locationAddress: 'Rustaveli Avenue 48',
  waiterId: 'waiter-1',
};

describe('CardReservationComponent', () => {
  let component: CardReservationComponent;
  let fixture: ComponentFixture<CardReservationComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      providers: [{ provide: API_BASE_URL, useValue: 'https://test.api' }, provideRouter([])],
      imports: [CardReservationComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(CardReservationComponent);
    component = fixture.componentInstance;
    // r — обов'язковий input: без нього компонент падає з NG0950
    fixture.componentRef.setInput('r', RESERVATION);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  function groupPreOrderButton(): HTMLButtonElement | undefined {
    const buttons = [...(fixture.nativeElement as HTMLElement).querySelectorAll('button')];
    return buttons.find((button) => button.textContent?.trim() === 'Group Pre-Order');
  }

  it('shows «Group Pre-Order» for a reserved reservation with 2+ guests and opens the owner screen', () => {
    const navigate = spyOn(TestBed.inject(Router), 'navigate').and.resolveTo(true);

    groupPreOrderButton()?.click();

    expect(navigate).toHaveBeenCalledWith(['/reservations', 'res-1', 'group-order']);
  });

  it('hides «Group Pre-Order» for a single-guest reservation', () => {
    fixture.componentRef.setInput('r', { ...RESERVATION, guestCount: 1 });
    fixture.detectChanges();

    expect(groupPreOrderButton()).toBeUndefined();
  });
});
