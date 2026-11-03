import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { EMPTY, Observable, Subscription, catchError, exhaustMap, finalize, forkJoin, interval, switchMap, tap } from 'rxjs';
import { GroupOrdersService } from '@app/services/group-orders/group-orders.service';
import { ToastService } from '@app/services/toast/toast.service';
import { GuestInvite } from '@app/models/reservations/guest-invite.model';
import { Participant } from '@app/models/group-orders/participant.model';
import { GroupReservationStatus, GroupStatusResponse } from '@app/models/group-orders/group-status.model';
import { GroupSessionsResponse } from '@app/models/group-orders/group-sessions-response.model';
import { consolidatePreOrder } from '@app/shared/helpers/consolidate-pre-order';
import { ParticipantCardComponent } from '@app/components/group-orders/participant-card/participant-card.component';
import { ModalComponent } from '@app/shared/custom-components/modal/modal.component';
import { HalloweenBackdropComponent } from '@app/components/group-orders/halloween-backdrop/halloween-backdrop.component';
import { LoadState } from './models';

/** Background sync period while the group is Open. */
export const GROUP_SYNC_INTERVAL_MS = 15_000;

const STATUS_LABELS: Record<GroupReservationStatus, string> = {
  Open: 'Collecting pre-orders',
  Submitted: 'Sent to Restaurant',
  Closed: 'Closed',
};

@Component({
  selector: 'app-group-order-page',
  imports: [ParticipantCardComponent, ModalComponent, HalloweenBackdropComponent, CurrencyPipe, DatePipe],
  templateUrl: './group-order-page.component.html',
  styleUrl: './group-order-page.component.scss',
  changeDetection: ChangeDetectionStrategy.Eager,
})
export class GroupOrderPageComponent {
  private readonly groupOrdersService = inject(GroupOrdersService);
  private readonly toastService = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly reservationId = inject(ActivatedRoute).snapshot.paramMap.get('reservationId') ?? '';

  private loadSubscription?: Subscription;
  private syncSubscription?: Subscription;

  readonly state = signal<LoadState>('loading');
  readonly invite = signal<GuestInvite | null>(null);
  readonly participants = signal<Participant[]>([]);
  readonly ownerSessionId = signal('');
  readonly groupStatus = signal<GroupReservationStatus>('Open');

  readonly lastUpdated = signal<Date | null>(null);
  readonly refreshFailed = signal(false);
  readonly syncing = signal(false);

  readonly confirmSubmitOpen = signal(false);
  readonly submitting = signal(false);

  readonly guestToRemove = signal<Participant | null>(null);
  readonly removing = signal(false);

  readonly consolidated = computed(() => consolidatePreOrder(this.participants()));
  readonly grandTotal = computed(() => this.consolidated().total);
  readonly statusLabel = computed(() => STATUS_LABELS[this.groupStatus()]);
  readonly isOpen = computed(() => this.groupStatus() === 'Open');
  readonly hasAnyDishes = computed(() => this.participants().some((person) => person.items.length > 0));
  // The Owner's own session exists from the moment the screen opens; while it is empty nobody is waiting on it.
  readonly pendingCount = computed(
    () =>
      this.participants().filter(
        (person) => !person.isSubmitted && (person.type !== 'Owner' || person.items.length > 0),
      ).length,
  );
  readonly pendingMessage = computed(() => {
    const count = this.pendingCount();
    return count === 1
      ? '1 participant hasn’t submitted their cart yet — their dishes won’t be included in the order'
      : `${count} participants haven’t submitted their cart yet — their dishes won’t be included in the order`;
  });
  readonly canSubmit = computed(() => this.isOpen() && this.consolidated().items.length > 0 && !this.submitting());

  constructor() {
    this.destroyRef.onDestroy(() => {
      this.loadSubscription?.unsubscribe();
      this.stopSync();
    });
    this.load();
  }

  load(): void {
    this.loadSubscription?.unsubscribe();
    this.stopSync();
    this.state.set('loading');

    this.loadSubscription = this.groupOrdersService
      .getGroupInvite$(this.reservationId)
      .pipe(
        switchMap((invite) =>
          forkJoin({
            sessions: this.groupOrdersService.getSessions$(invite.GroupReservationId),
            ownerSessionId: this.groupOrdersService.getOwnerSessionId$(this.reservationId),
            status: this.groupOrdersService.getStatus$(invite.GroupReservationId),
          }).pipe(tap(() => this.invite.set(invite))),
        ),
      )
      .subscribe({
        next: ({ sessions, ownerSessionId, status }) => {
          this.ownerSessionId.set(ownerSessionId);
          this.applySnapshot(sessions, status);
          this.state.set('ready');
          if (this.isOpen()) this.startSync();
        },
        error: (error: unknown) => {
          this.state.set(error instanceof HttpErrorResponse && error.status === 404 ? 'not-found' : 'error');
        },
      });
  }

  refresh(): void {
    if (this.syncing()) return;
    this.silentSync$().subscribe();
  }

  openSubmitDialog(): void {
    if (this.canSubmit()) this.confirmSubmitOpen.set(true);
  }

  closeSubmitDialog(): void {
    if (!this.submitting()) this.confirmSubmitOpen.set(false);
  }

  submit(): void {
    const invite = this.invite();
    if (!invite || this.submitting()) return;

    this.submitting.set(true);
    this.groupOrdersService
      .submit$(invite.GroupReservationId, this.reservationId)
      .pipe(
        finalize(() => this.submitting.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.groupStatus.set('Submitted');
          this.stopSync();
          this.confirmSubmitOpen.set(false);
          this.toastService.success('Pre-order sent to the restaurant');
        },
        // The error toast is already shown by tokenInterceptor.
        error: () => this.confirmSubmitOpen.set(false),
      });
  }

  askRemove(participant: Participant): void {
    this.guestToRemove.set(participant);
  }

  closeRemoveDialog(): void {
    if (!this.removing()) this.guestToRemove.set(null);
  }

  removeGuest(): void {
    const invite = this.invite();
    const guest = this.guestToRemove();
    if (!invite || !guest || this.removing()) return;

    this.removing.set(true);
    this.groupOrdersService
      .removeSession$(invite.GroupReservationId, guest.id)
      .pipe(
        finalize(() => this.removing.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (sessions) => {
          this.participants.set(sessions.participants);
          this.guestToRemove.set(null);
          this.toastService.success(`${guest.name} was removed from the reservation`);
        },
        error: () => this.guestToRemove.set(null),
      });
  }

  canRemove(participant: Participant): boolean {
    return this.isOpen() && participant.type === 'Guest' && participant.id !== this.ownerSessionId();
  }

  private startSync(): void {
    this.stopSync();
    this.syncSubscription = interval(GROUP_SYNC_INTERVAL_MS)
      .pipe(exhaustMap(() => this.silentSync$()))
      .subscribe();
  }

  private stopSync(): void {
    this.syncSubscription?.unsubscribe();
    this.syncSubscription = undefined;
  }

  /** Background refresh: no full-screen loader, no error toast, keeps the last data on failure. */
  private silentSync$(): Observable<unknown> {
    const invite = this.invite();
    if (!invite) return EMPTY;

    this.syncing.set(true);
    return forkJoin({
      sessions: this.groupOrdersService.getSessions$(invite.GroupReservationId, { silent: true }),
      status: this.groupOrdersService.getStatus$(invite.GroupReservationId, { silent: true }),
    }).pipe(
      tap(({ sessions, status }) => {
        this.applySnapshot(sessions, status);
        if (!this.isOpen()) this.stopSync();
      }),
      catchError(() => {
        this.refreshFailed.set(true);
        return EMPTY;
      }),
      finalize(() => this.syncing.set(false)),
    );
  }

  private applySnapshot(sessions: GroupSessionsResponse, status: GroupStatusResponse): void {
    this.participants.set(sessions.participants);
    this.groupStatus.set(status.status);
    this.lastUpdated.set(new Date());
    this.refreshFailed.set(false);
  }
}
