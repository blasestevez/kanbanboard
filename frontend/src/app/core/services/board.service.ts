import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  BoardDetail,
  BoardList,
  BoardSummary,
  CreateBoardRequest,
  CreateListRequest,
  ReorderListsRequest,
  UpdateBoardRequest,
  UpdateListRequest,
} from '../models/board.model';

@Injectable({
  providedIn: 'root',
})
export class BoardService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  private readonly _activeBoard = signal<BoardDetail | null>(null);
  private readonly _isLoading = signal<boolean>(false);

  readonly activeBoard = this._activeBoard.asReadonly();
  readonly isLoading = this._isLoading.asReadonly();

  getWorkspaceBoards(workspaceId: string): Observable<BoardSummary[]> {
    return this.http.get<BoardSummary[]>(
      `${this.apiUrl}/workspaces/${workspaceId}/boards`
    );
  }

  getBoard(id: string): Observable<BoardDetail> {
    this._isLoading.set(true);
    return this.http.get<BoardDetail>(`${this.apiUrl}/boards/${id}`).pipe(
      tap({
        next: (board) => {
          this._activeBoard.set(board);
          this._isLoading.set(false);
        },
        error: () => this._isLoading.set(false),
      })
    );
  }

  createBoard(
    workspaceId: string,
    req: CreateBoardRequest
  ): Observable<BoardSummary> {
    return this.http.post<BoardSummary>(
      `${this.apiUrl}/workspaces/${workspaceId}/boards`,
      req
    );
  }

  updateBoard(id: string, req: UpdateBoardRequest): Observable<BoardDetail> {
    return this.http.put<BoardDetail>(`${this.apiUrl}/boards/${id}`, req).pipe(
      tap((updated) => {
        this._activeBoard.set(updated);
      })
    );
  }

  deleteBoard(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/boards/${id}`).pipe(
      tap(() => {
        if (this._activeBoard()?.id === id) {
          this._activeBoard.set(null);
        }
      })
    );
  }

  createList(boardId: string, req: CreateListRequest): Observable<BoardList> {
    return this.http
      .post<BoardList>(`${this.apiUrl}/boards/${boardId}/lists`, req)
      .pipe(
        tap((newList) => {
          const board = this._activeBoard();
          if (board && board.id === boardId) {
            this._activeBoard.set({
              ...board,
              lists: [...board.lists, newList],
            });
          }
        })
      );
  }

  updateList(id: string, req: UpdateListRequest): Observable<BoardList> {
    return this.http.put<BoardList>(`${this.apiUrl}/lists/${id}`, req).pipe(
      tap((updatedList) => {
        const board = this._activeBoard();
        if (board) {
          this._activeBoard.set({
            ...board,
            lists: board.lists.map((l) => (l.id === id ? updatedList : l)),
          });
        }
      })
    );
  }

  reorderLists(boardId: string, listIds: string[]): Observable<void> {
    const payload: ReorderListsRequest = { listIds };
    return this.http.put<void>(
      `${this.apiUrl}/boards/${boardId}/lists/reorder`,
      payload
    );
  }

  deleteList(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/lists/${id}`).pipe(
      tap(() => {
        const board = this._activeBoard();
        if (board) {
          this._activeBoard.set({
            ...board,
            lists: board.lists.filter((l) => l.id !== id),
          });
        }
      })
    );
  }

  setActiveBoardLists(lists: BoardList[]): void {
    const board = this._activeBoard();
    if (board) {
      this._activeBoard.set({
        ...board,
        lists,
      });
    }
  }
}
