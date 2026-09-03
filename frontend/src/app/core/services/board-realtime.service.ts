import { Injectable, inject, signal } from '@angular/core';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import { BoardList, CardSummary } from '../models/board.model';
import { CardDetail } from '../models/card.model';

export interface BoardUpdatedEvent {
  title: string;
  backgroundColor?: string | null;
  backgroundImageUrl?: string | null;
  isClosed: boolean;
}

export interface CardMovedEvent {
  cardId: string;
  sourceListId: string;
  targetListId: string;
  newPosition: number;
}

export interface CardDeletedEvent {
  cardId: string;
  listId: string;
}

@Injectable({
  providedIn: 'root',
})
export class BoardRealtimeService {
  private readonly authService = inject(AuthService);

  private hubConnection: HubConnection | null = null;
  private currentBoardId: string | null = null;

  private readonly _isConnected = signal<boolean>(false);
  readonly isConnected = this._isConnected.asReadonly();

  // Subjects for server events
  private readonly boardUpdatedSubject = new Subject<BoardUpdatedEvent>();
  private readonly listCreatedSubject = new Subject<BoardList>();
  private readonly listUpdatedSubject = new Subject<BoardList>();
  private readonly listDeletedSubject = new Subject<string>();
  private readonly listsReorderedSubject = new Subject<string[]>();
  private readonly cardCreatedSubject = new Subject<CardDetail | CardSummary>();
  private readonly cardUpdatedSubject = new Subject<CardDetail>();
  private readonly cardMovedSubject = new Subject<CardMovedEvent>();
  private readonly cardDeletedSubject = new Subject<CardDeletedEvent>();

  // Public Observables
  readonly boardUpdated$ = this.boardUpdatedSubject.asObservable();
  readonly listCreated$ = this.listCreatedSubject.asObservable();
  readonly listUpdated$ = this.listUpdatedSubject.asObservable();
  readonly listDeleted$ = this.listDeletedSubject.asObservable();
  readonly listsReordered$ = this.listsReorderedSubject.asObservable();
  readonly cardCreated$ = this.cardCreatedSubject.asObservable();
  readonly cardUpdated$ = this.cardUpdatedSubject.asObservable();
  readonly cardMoved$ = this.cardMovedSubject.asObservable();
  readonly cardDeleted$ = this.cardDeletedSubject.asObservable();

  private getHubUrl(): string {
    const base = environment.apiUrl.replace(/\/api\/?$/, '');
    return `${base}/hubs/board`;
  }

  private initConnection(): void {
    if (this.hubConnection) return;

    this.hubConnection = new HubConnectionBuilder()
      .withUrl(this.getHubUrl(), {
        accessTokenFactory: () => this.authService.getToken() || '',
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();

    this.hubConnection.on('BoardUpdated', (data: BoardUpdatedEvent) => {
      this.boardUpdatedSubject.next(data);
    });

    this.hubConnection.on('ListCreated', (data: BoardList) => {
      this.listCreatedSubject.next(data);
    });

    this.hubConnection.on('ListUpdated', (data: BoardList) => {
      this.listUpdatedSubject.next(data);
    });

    this.hubConnection.on('ListDeleted', (data: any) => {
      const listId = typeof data === 'string' ? data : (data?.listId ?? String(data));
      this.listDeletedSubject.next(listId);
    });

    this.hubConnection.on('ListsReordered', (data: string[]) => {
      this.listsReorderedSubject.next(data);
    });

    this.hubConnection.on('CardCreated', (data: CardDetail | CardSummary) => {
      this.cardCreatedSubject.next(data);
    });

    this.hubConnection.on('CardUpdated', (data: CardDetail) => {
      this.cardUpdatedSubject.next(data);
    });

    this.hubConnection.on('CardMoved', (data: CardMovedEvent) => {
      this.cardMovedSubject.next(data);
    });

    this.hubConnection.on('CardDeleted', (data: any) => {
      const payload: CardDeletedEvent = {
        cardId: data?.cardId ?? (typeof data === 'string' ? data : ''),
        listId: data?.listId ?? '',
      };
      this.cardDeletedSubject.next(payload);
    });

    this.hubConnection.onreconnecting(() => {
      this._isConnected.set(false);
    });

    this.hubConnection.onreconnected(() => {
      this._isConnected.set(true);
      if (this.currentBoardId && this.hubConnection) {
        this.hubConnection.invoke('JoinBoard', this.currentBoardId).catch(console.error);
      }
    });

    this.hubConnection.onclose(() => {
      this._isConnected.set(false);
    });
  }

  async joinBoard(boardId: string): Promise<void> {
    this.currentBoardId = boardId;
    this.initConnection();

    if (!this.hubConnection) return;

    if (this.hubConnection.state === HubConnectionState.Disconnected) {
      try {
        await this.hubConnection.start();
        this._isConnected.set(true);
      } catch (err) {
        console.warn('Could not connect to SignalR board hub:', err);
        this._isConnected.set(false);
        return;
      }
    }

    if (this.hubConnection.state === HubConnectionState.Connected) {
      try {
        await this.hubConnection.invoke('JoinBoard', boardId);
      } catch (err) {
        console.warn('Failed to invoke JoinBoard on hub:', err);
      }
    }
  }

  async leaveBoard(boardId: string): Promise<void> {
    if (this.currentBoardId === boardId) {
      this.currentBoardId = null;
    }

    if (this.hubConnection && this.hubConnection.state === HubConnectionState.Connected) {
      try {
        await this.hubConnection.invoke('LeaveBoard', boardId);
      } catch (err) {
        console.warn('Failed to invoke LeaveBoard on hub:', err);
      }
    }
  }

  async disconnect(): Promise<void> {
    this.currentBoardId = null;
    if (this.hubConnection) {
      try {
        await this.hubConnection.stop();
      } catch (err) {
        console.warn('Error stopping SignalR hub connection:', err);
      } finally {
        this._isConnected.set(false);
        this.hubConnection = null;
      }
    }
  }
}
