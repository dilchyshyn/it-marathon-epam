import { HttpClient, HttpContext } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, map, switchMap } from 'rxjs';
import { GroupSessionsResponse } from '@app/models/group-orders/group-sessions-response.model';
import { GroupStatusResponse } from '@app/models/group-orders/group-status.model';
import { OwnerSessionResponse } from '@app/models/group-orders/owner-session.model';
import { RequestOptions } from '@app/models/group-orders/request-options.model';
import { SubmitGroupPreOrderRequest, SubmittedDish } from '@app/models/group-orders/submit-group-pre-order.model';
import { GuestInvite } from '@app/models/reservations/guest-invite.model';
import { SKIP_ERROR_TOAST } from '@app/auth/skip-error-toast.context';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';
import { GuestInviteService } from '@app/services/reservation/guest-invite.service';

@Injectable({
  providedIn: 'root',
})
export class GroupOrdersService {
  private readonly baseUrl = inject(API_BASE_URL);
  private readonly http = inject(HttpClient);
  private readonly guestInviteService = inject(GuestInviteService);

  /** reservationId → invite token → invite (GroupReservationId and header data). */
  getGroupInvite$(reservationId: string): Observable<GuestInvite> {
    return this.guestInviteService
      .getInviteToken$(reservationId)
      .pipe(switchMap(({ token }) => this.guestInviteService.getInvite$(token)));
  }

  getSessions$(groupReservationId: string, options: RequestOptions = {}): Observable<GroupSessionsResponse> {
    return this.http.get<GroupSessionsResponse>(`${this.groupUrl(groupReservationId)}/sessions`, {
      context: this.context(options),
    });
  }

  getOwnerSessionId$(reservationId: string): Observable<string> {
    return this.http
      .get<OwnerSessionResponse>(`${this.baseUrl}/reservations/${encodeURIComponent(reservationId)}/guest-session-id`)
      .pipe(map((response) => response.guestSessionId));
  }

  getStatus$(groupReservationId: string, options: RequestOptions = {}): Observable<GroupStatusResponse> {
    return this.http.get<GroupStatusResponse>(`${this.groupUrl(groupReservationId)}/status`, {
      context: this.context(options),
    });
  }

  submit$(groupReservationId: string, reservationId: string): Observable<SubmittedDish[]> {
    const body: SubmitGroupPreOrderRequest = { reservationId };
    return this.http.post<SubmittedDish[]>(
      `${this.baseUrl}/group-reservations/${encodeURIComponent(groupReservationId)}/submit`,
      body,
    );
  }

  removeSession$(groupReservationId: string, guestSessionId: string): Observable<GroupSessionsResponse> {
    return this.http.delete<GroupSessionsResponse>(
      `${this.groupUrl(groupReservationId)}/sessions/${encodeURIComponent(guestSessionId)}`,
    );
  }

  private groupUrl(groupReservationId: string): string {
    return `${this.baseUrl}/guest/groups/${encodeURIComponent(groupReservationId)}`;
  }

  private context(options: RequestOptions): HttpContext {
    return new HttpContext().set(SKIP_ERROR_TOAST, options.silent ?? false);
  }
}
