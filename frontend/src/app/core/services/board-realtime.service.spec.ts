import { TestBed } from '@angular/core/testing';
import { BoardRealtimeService } from './board-realtime.service';
import { AuthService } from './auth.service';
import { vi } from 'vitest';

describe('BoardRealtimeService', () => {
  let service: BoardRealtimeService;
  let authServiceSpy: { getToken: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    authServiceSpy = {
      getToken: vi.fn().mockReturnValue('fake-token'),
    };

    TestBed.configureTestingModule({
      providers: [
        BoardRealtimeService,
        { provide: AuthService, useValue: authServiceSpy },
      ],
    });

    service = TestBed.inject(BoardRealtimeService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
    expect(service.isConnected()).toBe(false);
  });

  it('should have public observables for all board events', () => {
    expect(service.boardUpdated$).toBeDefined();
    expect(service.listCreated$).toBeDefined();
    expect(service.listUpdated$).toBeDefined();
    expect(service.listDeleted$).toBeDefined();
    expect(service.listsReordered$).toBeDefined();
    expect(service.cardCreated$).toBeDefined();
    expect(service.cardUpdated$).toBeDefined();
    expect(service.cardMoved$).toBeDefined();
    expect(service.cardDeleted$).toBeDefined();
  });

  it('should cleanly disconnect without errors if not connected', async () => {
    await expect(service.disconnect()).resolves.toBeUndefined();
    expect(service.isConnected()).toBe(false);
  });

  it('should cleanly leaveBoard without errors if not connected', async () => {
    await expect(service.leaveBoard('board-1')).resolves.toBeUndefined();
  });
});
