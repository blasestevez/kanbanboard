import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  BoardDetail,
  BoardList,
  BoardSummary,
  CardSummary,
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
  private get apiUrl(): string {
    return environment.apiUrl;
  }

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

  updateActiveBoard(patch: Partial<BoardDetail>): void {
    const b = this._activeBoard();
    if (b) {
      this._activeBoard.set({
        ...b,
        ...patch,
      });
    }
  }

  addListToActiveBoard(newList: BoardList): void {
    const b = this._activeBoard();
    if (!b) return;
    if (b.lists.some((l) => l.id === newList.id)) return;
    const cards = newList.cards ?? [];
    this._activeBoard.set({
      ...b,
      lists: [...b.lists, { ...newList, cards }],
    });
  }

  updateListInActiveBoard(updatedList: BoardList): void {
    const b = this._activeBoard();
    if (!b) return;
    this._activeBoard.set({
      ...b,
      lists: b.lists.map((l) =>
        l.id === updatedList.id
          ? {
              ...l,
              title: updatedList.title,
              isArchived: updatedList.isArchived,
              position: updatedList.position,
            }
          : l
      ),
    });
  }

  removeListFromActiveBoard(listId: string): void {
    const b = this._activeBoard();
    if (!b) return;
    this._activeBoard.set({
      ...b,
      lists: b.lists.filter((l) => l.id !== listId),
    });
  }

  reorderListsInActiveBoard(listIds: string[]): void {
    const b = this._activeBoard();
    if (!b) return;
    const map = new Map(b.lists.map((l) => [l.id, l]));
    const newLists: BoardList[] = [];
    for (const id of listIds) {
      const item = map.get(id);
      if (item) {
        newLists.push(item);
        map.delete(id);
      }
    }
    for (const remaining of map.values()) {
      newLists.push(remaining);
    }
    this._activeBoard.set({
      ...b,
      lists: newLists,
    });
  }

  private toCardSummary(card: CardSummary | any): CardSummary {
    const commentsCount =
      'comments' in card && Array.isArray(card.comments)
        ? card.comments.length
        : ('commentsCount' in card
          ? card.commentsCount
          : 0);
    const checklistItemsTotal =
      'checklists' in card && Array.isArray(card.checklists)
        ? card.checklists.reduce(
            (sum: number, chk: any) => sum + (chk.items?.length || 0),
            0
          )
        : ('checklistItemsTotal' in card
          ? card.checklistItemsTotal
          : 0);
    const checklistItemsChecked =
      'checklists' in card && Array.isArray(card.checklists)
        ? card.checklists.reduce(
            (sum: number, chk: any) =>
              sum + (chk.items?.filter((i: any) => i.isChecked)?.length || 0),
            0
          )
        : ('checklistItemsChecked' in card
          ? card.checklistItemsChecked
          : 0);

    return {
      id: card.id,
      listId: card.listId,
      title: card.title,
      description: card.description,
      position: card.position ?? 0,
      dueDate: card.dueDate,
      isComplete: card.isComplete ?? false,
      coverColor: card.coverColor,
      coverImageUrl: card.coverImageUrl,
      commentsCount,
      checklistItemsTotal,
      checklistItemsChecked,
    };
  }

  addOrUpdateCardInActiveBoard(card: CardSummary | any): void {
    const b = this._activeBoard();
    if (!b) return;

    const summary = this.toCardSummary(card);
    let cardFound = false;

    const updatedLists = b.lists.map((l) => {
      const idx = l.cards.findIndex((c) => c.id === summary.id);
      if (idx !== -1) {
        cardFound = true;
        if (summary.listId && summary.listId !== l.id) {
          return {
            ...l,
            cards: l.cards.filter((c) => c.id !== summary.id),
          };
        }
        const newCards = [...l.cards];
        newCards[idx] = { ...newCards[idx], ...summary };
        return { ...l, cards: newCards };
      }
      return {
        ...l,
        cards: [...l.cards],
      };
    });

    if (cardFound) {
      const targetList = updatedLists.find((l) => l.id === summary.listId);
      if (targetList && !targetList.cards.some((c) => c.id === summary.id)) {
        targetList.cards.push(summary);
      }
    } else {
      const targetList = updatedLists.find((l) => l.id === summary.listId);
      if (targetList) {
        targetList.cards.push(summary);
      }
    }

    this._activeBoard.set({
      ...b,
      lists: updatedLists,
    });
  }

  moveCardInActiveBoard(
    cardId: string,
    sourceListId: string,
    targetListId: string,
    newPosition: number
  ): void {
    const b = this._activeBoard();
    if (!b) return;

    let foundCard: CardSummary | null = null;
    const newLists = b.lists.map((l) => {
      const cardIdx = l.cards.findIndex((c) => c.id === cardId);
      if (cardIdx !== -1) {
        foundCard = { ...l.cards[cardIdx] };
        return {
          ...l,
          cards: l.cards.filter((c) => c.id !== cardId),
        };
      }
      return {
        ...l,
        cards: [...l.cards],
      };
    });

    if (!foundCard) return;

    (foundCard as CardSummary).listId = targetListId;
    (foundCard as CardSummary).position = newPosition;

    const targetList = newLists.find((l) => l.id === targetListId);
    if (targetList) {
      const clampedPos = Math.max(
        0,
        Math.min(newPosition, targetList.cards.length)
      );
      targetList.cards.splice(clampedPos, 0, foundCard);
    }

    this._activeBoard.set({
      ...b,
      lists: newLists,
    });
  }

  removeCardFromActiveBoard(cardId: string, listId?: string): void {
    const b = this._activeBoard();
    if (!b) return;

    this._activeBoard.set({
      ...b,
      lists: b.lists.map((l) => {
        if (!listId || l.id === listId) {
          return {
            ...l,
            cards: l.cards.filter((c) => c.id !== cardId),
          };
        }
        return l;
      }),
    });
  }
}

