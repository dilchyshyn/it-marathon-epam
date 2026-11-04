import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { GuestInvite } from '@app/models/reservations/guest-invite.model';
import { GuestInviteService } from '@app/services/reservation/guest-invite.service';
import { GuestSessionsService } from '@app/services/guest-sessions/guest-sessions.service';
import { GuestPreorderStore } from './guest-preorder.store';

const groupFull = new HttpErrorResponse({
  status: 400,
  error: { message: 'Guest limit exceeded', errorCode: 'GuestLimitExceeded' },
});
const serverDown = new HttpErrorResponse({ status: 500, error: {} });
const INVITE = { GroupReservationId: 'group-1', LocationId: 'location-1' } as unknown as GuestInvite;

describe('GuestPreorderStore: a full group', () => {
  let invites: jasmine.SpyObj<GuestInviteService>;
  let sessions: jasmine.SpyObj<GuestSessionsService>;
  let store: GuestPreorderStore;

  beforeEach(() => {
    localStorage.clear();
    spyOn(console, 'log');
    invites = jasmine.createSpyObj<GuestInviteService>('GuestInviteService', ['getInvite$']);
    sessions = jasmine.createSpyObj<GuestSessionsService>('GuestSessionsService', ['createSession', 'getMenu']);
    TestBed.configureTestingModule({
      providers: [
        GuestPreorderStore,
        { provide: GuestInviteService, useValue: invites },
        { provide: GuestSessionsService, useValue: sessions },
      ],
    });
    store = TestBed.inject(GuestPreorderStore);
  });

  afterEach(() => localStorage.clear());

  it('says the group is full when the invitation is refused for that reason', async () => {
    invites.getInvite$.and.returnValue(throwError(() => groupFull));

    await store.loadInviteByToken('token');

    expect(store.inviteError()).toBe('full');
  });

  it('still offers a retry for a temporary failure', async () => {
    invites.getInvite$.and.returnValue(throwError(() => serverDown));

    await store.loadInviteByToken('token');

    expect(store.inviteError()).toBe('retryable');
  });

  it('says the group is full when joining is refused, and does not keep the name', async () => {
    invites.getInvite$.and.returnValue(of(INVITE));
    sessions.createSession.and.returnValue(throwError(() => groupFull));
    await store.loadInviteByToken('token');

    await store.join('Anna');

    expect(store.inviteError()).toBe('full');
    expect(store.guestName()).toBe('');
    expect(store.joinError()).toBeNull();
  });

  it('reports other join failures as before', async () => {
    invites.getInvite$.and.returnValue(of(INVITE));
    sessions.createSession.and.returnValue(throwError(() => serverDown));
    await store.loadInviteByToken('token');

    await store.join('Anna');

    expect(store.inviteError()).toBeNull();
    expect(store.joinError()).toBe('Failed to join session');
  });
});
