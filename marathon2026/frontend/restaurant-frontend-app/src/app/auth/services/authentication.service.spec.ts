import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { AuthenticationService } from './authentication.service';
import { tokenInterceptor } from '../token.interceptor';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';

const API = 'https://test.api';

function tokenExpiringIn(seconds: number): string {
  const payload = btoa(JSON.stringify({ exp: Math.floor(Date.now() / 1000) + seconds }));
  return `header.${payload}.signature`;
}

describe('AuthenticationService', () => {
  function create() {
    TestBed.configureTestingModule({
      providers: [
        { provide: API_BASE_URL, useValue: API },
        provideHttpClient(withInterceptors([tokenInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    const service = TestBed.inject(AuthenticationService);
    service.restoreSession();
    return { service, http: TestBed.inject(HttpTestingController) };
  }

  function storeSession(token: string, user?: object): void {
    localStorage.setItem('auth', JSON.stringify({ idToken: token, accessToken: 'access' }));
    if (user) {
      localStorage.setItem('user', JSON.stringify(user));
    }
  }

  beforeEach(() => {
    localStorage.clear();
    spyOn(console, 'error');
    spyOn(console, 'warn');
  });

  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
    localStorage.clear();
  });

  it('should be created', () => {
    expect(create().service).toBeTruthy();
  });

  it('loads the user on startup when a valid token has no stored user', async () => {
    storeSession(tokenExpiringIn(3600));
    const { service, http } = create();

    const request = http.expectOne(`${API}/me`);
    expect(request.request.headers.get('Authorization')).toContain('Bearer ');
    request.flush({ id: 'user-1', firstName: 'Alice' });

    expect((await firstValueFrom(service.user$))?.id).toBe('user-1');
    expect(await firstValueFrom(service.isAuthorised$)).toBeTrue();
  });

  it('signs out when a token has no stored user and the user cannot be loaded', async () => {
    storeSession(tokenExpiringIn(3600));
    const { service, http } = create();

    http.expectOne(`${API}/me`).flush({}, { status: 500, statusText: 'Server Error' });

    expect(await firstValueFrom(service.user$)).toBeNull();
    expect(await firstValueFrom(service.isAuthorised$)).toBeFalse();
    expect(localStorage.getItem('auth')).toBeNull();
  });

  it('signs out on startup when the stored token has expired', async () => {
    storeSession(tokenExpiringIn(-60), { id: 'user-1' });
    const { service } = create();

    expect(await firstValueFrom(service.user$)).toBeNull();
    expect(await firstValueFrom(service.isAuthorised$)).toBeFalse();
  });

  it('signs out on startup instead of failing when the stored token is damaged', async () => {
    storeSession('not-a-token', { id: 'user-1' });
    const { service } = create();

    expect(await firstValueFrom(service.isAuthorised$)).toBeFalse();
    expect(localStorage.getItem('auth')).toBeNull();
  });

  it('keeps a stored user with a valid token without calling the server on startup', async () => {
    storeSession(tokenExpiringIn(3600), { id: 'user-1' });
    const { service, http } = create();

    http.expectNone(`${API}/me`);

    expect((await firstValueFrom(service.user$))?.id).toBe('user-1');
  });

  it('keeps the stored user when refreshing it fails for a moment', async () => {
    storeSession(tokenExpiringIn(3600), { id: 'user-1' });
    const { service, http } = create();

    service.editProfile('Alice', 'Smith').subscribe();
    http.expectOne(`${API}/me`).flush({});
    http.expectOne(`${API}/me`).flush({}, { status: 500, statusText: 'Server Error' });

    expect((await firstValueFrom(service.user$))?.id).toBe('user-1');
    expect(await firstValueFrom(service.isAuthorised$)).toBeTrue();
  });
});
