import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { CardService } from './card.service';
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

describe('CardService', () => {
  let service: CardService;
  let httpMock: HttpTestingController;
  const apiUrl = environment.apiUrl;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        CardService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(CardService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should create a card', () => {
    const createReq: CreateCardRequest = {
      title: 'New Card',
      description: 'Desc',
      coverColor: '#0079bf',
    };
    const mockCreated: CardDetail = {
      id: 'card-1',
      listId: 'list-1',
      boardId: 'board-1',
      title: 'New Card',
      description: 'Desc',
      position: 0,
      dueDate: null,
      isComplete: false,
      coverColor: '#0079bf',
      coverImageUrl: null,
      createdAt: '2026-09-02T20:00:00Z',
      members: [],
      labels: [],
      checklists: [],
      comments: [],
      attachments: [],
    };

    service.createCard('list-1', createReq).subscribe((res) => {
      expect(res).toEqual(mockCreated);
    });

    const req = httpMock.expectOne(`${apiUrl}/lists/list-1/cards`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(createReq);
    req.flush(mockCreated);
  });

  it('should get card detail and update activeCard signal', () => {
    const mockCard: CardDetail = {
      id: 'card-1',
      listId: 'list-1',
      boardId: 'board-1',
      title: 'New Card',
      description: 'Desc',
      position: 0,
      dueDate: null,
      isComplete: false,
      coverColor: null,
      coverImageUrl: null,
      createdAt: '2026-09-02T20:00:00Z',
      members: [],
      labels: [],
      checklists: [],
      comments: [],
      attachments: [],
    };

    service.getCard('card-1').subscribe((res) => {
      expect(res).toEqual(mockCard);
      expect(service.activeCard()).toEqual(mockCard);
      expect(service.isLoading()).toBe(false);
    });

    const req = httpMock.expectOne(`${apiUrl}/cards/card-1`);
    expect(req.request.method).toBe('GET');
    req.flush(mockCard);
  });

  it('should update card and sync activeCard', () => {
    const updateReq: UpdateCardRequest = {
      title: 'Updated Card',
      description: 'Updated Desc',
      dueDate: null,
      isComplete: true,
      coverColor: '#519839',
      coverImageUrl: null,
    };
    const mockUpdated: CardDetail = {
      id: 'card-1',
      listId: 'list-1',
      boardId: 'board-1',
      title: 'Updated Card',
      description: 'Updated Desc',
      position: 0,
      dueDate: null,
      isComplete: true,
      coverColor: '#519839',
      coverImageUrl: null,
      createdAt: '2026-09-02T20:00:00Z',
      members: [],
      labels: [],
      checklists: [],
      comments: [],
      attachments: [],
    };

    service.updateCard('card-1', updateReq).subscribe((res) => {
      expect(res).toEqual(mockUpdated);
    });

    const req = httpMock.expectOne(`${apiUrl}/cards/card-1`);
    expect(req.request.method).toBe('PUT');
    req.flush(mockUpdated);
  });

  it('should move a card', () => {
    const moveReq: MoveCardRequest = {
      targetListId: 'list-2',
      newPosition: 1,
    };

    service.moveCard('card-1', moveReq).subscribe();

    const req = httpMock.expectOne(`${apiUrl}/cards/card-1/move`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(moveReq);
    req.flush(null);
  });

  it('should delete a card', () => {
    service.deleteCard('card-1').subscribe();

    const req = httpMock.expectOne(`${apiUrl}/cards/card-1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('should manage board labels', () => {
    const mockLabels: CardLabel[] = [
      { id: 'lbl-1', name: 'Feature', color: '#61bd4f' },
    ];
    service.getBoardLabels('board-1').subscribe((res) => {
      expect(res).toEqual(mockLabels);
    });
    httpMock.expectOne(`${apiUrl}/boards/board-1/labels`).flush(mockLabels);

    const createReq: CreateLabelRequest = { name: 'Bug', color: '#eb5a46' };
    const mockCreatedLabel: CardLabel = { id: 'lbl-2', name: 'Bug', color: '#eb5a46' };
    service.createLabel('board-1', createReq).subscribe((res) => {
      expect(res).toEqual(mockCreatedLabel);
    });
    httpMock.expectOne(`${apiUrl}/boards/board-1/labels`).flush(mockCreatedLabel);

    service.assignLabel('card-1', 'lbl-1').subscribe();
    httpMock.expectOne(`${apiUrl}/cards/card-1/labels/lbl-1`).flush(null);

    service.removeLabel('card-1', 'lbl-1').subscribe();
    httpMock.expectOne(`${apiUrl}/cards/card-1/labels/lbl-1`).flush(null);
  });

  it('should manage checklists and items', () => {
    const createChkReq: CreateChecklistRequest = { title: 'Todos' };
    const mockChecklist: Checklist = {
      id: 'chk-1',
      title: 'Todos',
      position: 0,
      items: [],
    };
    service.createChecklist('card-1', createChkReq).subscribe((res) => {
      expect(res).toEqual(mockChecklist);
    });
    httpMock.expectOne(`${apiUrl}/cards/card-1/checklists`).flush(mockChecklist);

    service.deleteChecklist('chk-1').subscribe();
    httpMock.expectOne(`${apiUrl}/checklists/chk-1`).flush(null);

    const createItemReq: CreateChecklistItemRequest = { text: 'Item 1' };
    const mockItem: ChecklistItem = {
      id: 'item-1',
      text: 'Item 1',
      isChecked: false,
      position: 0,
    };
    service.createItem('chk-1', createItemReq).subscribe((res) => {
      expect(res).toEqual(mockItem);
    });
    httpMock.expectOne(`${apiUrl}/checklists/chk-1/items`).flush(mockItem);

    const updateItemReq: UpdateChecklistItemRequest = { text: 'Item 1 Edited', isChecked: true };
    service.updateItem('item-1', updateItemReq).subscribe((res) => {
      expect(res.isChecked).toBe(true);
    });
    httpMock.expectOne(`${apiUrl}/checklist-items/item-1`).flush({
      ...mockItem,
      ...updateItemReq,
    });

    service.deleteItem('item-1').subscribe();
    httpMock.expectOne(`${apiUrl}/checklist-items/item-1`).flush(null);
  });

  it('should manage comments', () => {
    const addCommentReq: CreateCommentRequest = { text: 'Great progress' };
    const mockComment: CardComment = {
      id: 'com-1',
      authorId: 'user-1',
      authorName: 'Alice',
      text: 'Great progress',
      createdAt: '2026-09-02T20:00:00Z',
    };
    service.addComment('card-1', addCommentReq).subscribe((res) => {
      expect(res).toEqual(mockComment);
    });
    httpMock.expectOne(`${apiUrl}/cards/card-1/comments`).flush(mockComment);

    const updateCommentReq: UpdateCommentRequest = { text: 'Updated comment' };
    service.updateComment('com-1', updateCommentReq).subscribe((res) => {
      expect(res.text).toBe('Updated comment');
    });
    httpMock.expectOne(`${apiUrl}/comments/com-1`).flush({
      ...mockComment,
      text: 'Updated comment',
    });

    service.deleteComment('com-1').subscribe();
    httpMock.expectOne(`${apiUrl}/comments/com-1`).flush(null);
  });

  it('should manage attachments and card members', () => {
    const mockAttachment: CardAttachment = {
      id: 'att-1',
      fileName: 'doc.pdf',
      fileUrl: '/uploads/doc.pdf',
      contentType: 'application/pdf',
      fileSizeBytes: 1024,
      createdAt: '2026-09-02T20:00:00Z',
    };
    const file = new File(['hello'], 'doc.pdf', { type: 'application/pdf' });
    service.uploadAttachment('card-1', file).subscribe((res) => {
      expect(res).toEqual(mockAttachment);
    });
    const uploadReq = httpMock.expectOne(`${apiUrl}/cards/card-1/attachments`);
    expect(uploadReq.request.method).toBe('POST');
    uploadReq.flush(mockAttachment);

    service.deleteAttachment('att-1').subscribe();
    httpMock.expectOne(`${apiUrl}/attachments/att-1`).flush(null);

    service.assignMember('card-1', 'user-2').subscribe();
    httpMock.expectOne(`${apiUrl}/cards/card-1/members/user-2`).flush(null);

    service.removeMember('card-1', 'user-2').subscribe();
    httpMock.expectOne(`${apiUrl}/cards/card-1/members/user-2`).flush(null);
  });
});
