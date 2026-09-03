import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CardAttachment,
  CardComment,
  CardDetail,
  CardLabel,
  Checklist,
  ChecklistItem,
  CreateCardRequest,
  CreateChecklistItemRequest,
  CreateChecklistRequest,
  CreateCommentRequest,
  CreateLabelRequest,
  MoveCardRequest,
  UpdateCardRequest,
  UpdateChecklistItemRequest,
  UpdateCommentRequest,
} from '../models/card.model';

@Injectable({
  providedIn: 'root',
})
export class CardService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  private readonly _activeCard = signal<CardDetail | null>(null);
  private readonly _isLoading = signal<boolean>(false);

  readonly activeCard = this._activeCard.asReadonly();
  readonly isLoading = this._isLoading.asReadonly();

  // Cards CRUD
  createCard(listId: string, req: CreateCardRequest): Observable<CardDetail> {
    return this.http.post<CardDetail>(
      `${this.apiUrl}/lists/${listId}/cards`,
      req
    );
  }

  getCard(id: string): Observable<CardDetail> {
    this._isLoading.set(true);
    return this.http.get<CardDetail>(`${this.apiUrl}/cards/${id}`).pipe(
      tap({
        next: (card) => {
          this._activeCard.set(card);
          this._isLoading.set(false);
        },
        error: () => this._isLoading.set(false),
      })
    );
  }

  updateCard(id: string, req: UpdateCardRequest): Observable<CardDetail> {
    return this.http.put<CardDetail>(`${this.apiUrl}/cards/${id}`, req).pipe(
      tap((updated) => {
        if (this._activeCard()?.id === id) {
          this._activeCard.set(updated);
        }
      })
    );
  }

  moveCard(id: string, req: MoveCardRequest): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/cards/${id}/move`, req);
  }

  deleteCard(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/cards/${id}`).pipe(
      tap(() => {
        if (this._activeCard()?.id === id) {
          this._activeCard.set(null);
        }
      })
    );
  }

  // Labels
  getBoardLabels(boardId: string): Observable<CardLabel[]> {
    return this.http.get<CardLabel[]>(
      `${this.apiUrl}/boards/${boardId}/labels`
    );
  }

  createLabel(boardId: string, req: CreateLabelRequest): Observable<CardLabel> {
    return this.http.post<CardLabel>(
      `${this.apiUrl}/boards/${boardId}/labels`,
      req
    );
  }

  assignLabel(cardId: string, labelId: string): Observable<void> {
    return this.http.post<void>(
      `${this.apiUrl}/cards/${cardId}/labels/${labelId}`,
      {}
    );
  }

  removeLabel(cardId: string, labelId: string): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/cards/${cardId}/labels/${labelId}`
    );
  }

  // Checklists
  createChecklist(
    cardId: string,
    req: CreateChecklistRequest
  ): Observable<Checklist> {
    return this.http.post<Checklist>(
      `${this.apiUrl}/cards/${cardId}/checklists`,
      req
    );
  }

  deleteChecklist(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/checklists/${id}`);
  }

  createItem(
    checklistId: string,
    req: CreateChecklistItemRequest
  ): Observable<ChecklistItem> {
    return this.http.post<ChecklistItem>(
      `${this.apiUrl}/checklists/${checklistId}/items`,
      req
    );
  }

  updateItem(
    id: string,
    req: UpdateChecklistItemRequest
  ): Observable<ChecklistItem> {
    return this.http.put<ChecklistItem>(
      `${this.apiUrl}/checklist-items/${id}`,
      req
    );
  }

  deleteItem(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/checklist-items/${id}`);
  }

  // Comments
  addComment(
    cardId: string,
    req: CreateCommentRequest
  ): Observable<CardComment> {
    return this.http.post<CardComment>(
      `${this.apiUrl}/cards/${cardId}/comments`,
      req
    );
  }

  updateComment(
    id: string,
    req: UpdateCommentRequest
  ): Observable<CardComment> {
    return this.http.put<CardComment>(
      `${this.apiUrl}/comments/${id}`,
      req
    );
  }

  deleteComment(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/comments/${id}`);
  }

  // Attachments
  uploadAttachment(
    cardId: string,
    file: File
  ): Observable<CardAttachment> {
    const formData = new FormData();
    formData.append('file', file, file.name);

    return this.http.post<CardAttachment>(
      `${this.apiUrl}/cards/${cardId}/attachments`,
      formData
    );
  }

  deleteAttachment(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/attachments/${id}`);
  }

  // Card Members
  assignMember(cardId: string, userId: string): Observable<void> {
    return this.http.post<void>(
      `${this.apiUrl}/cards/${cardId}/members/${userId}`,
      {}
    );
  }

  removeMember(cardId: string, userId: string): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/cards/${cardId}/members/${userId}`
    );
  }

  setActiveCard(card: CardDetail | null): void {
    this._activeCard.set(card);
  }
}
