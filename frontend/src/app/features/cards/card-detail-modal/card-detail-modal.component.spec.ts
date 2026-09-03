import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { CardDetailModalComponent } from './card-detail-modal.component';
import { CardService } from '../../../core/services/card.service';
import { AuthService } from '../../../core/services/auth.service';
import { CardDetail } from '../../../core/models/card.model';
import { BoardDetail } from '../../../core/models/board.model';
import { User } from '../../../core/models/auth.model';

describe('CardDetailModalComponent', () => {
  let component: CardDetailModalComponent;
  let fixture: ComponentFixture<CardDetailModalComponent>;

  const mockCard: CardDetail = {
    id: 'card-1',
    listId: 'list-1',
    boardId: 'board-1',
    title: 'Setup Database',
    description: 'Initial migration and schema',
    position: 0,
    dueDate: '2026-09-15T00:00:00Z',
    isComplete: false,
    coverColor: '#0079bf',
    coverImageUrl: null,
    createdAt: '2026-09-02T20:00:00Z',
    members: [
      {
        userId: 'user-1',
        fullName: 'Alice Dev',
        email: 'alice@example.com',
        avatarUrl: null,
      },
    ],
    labels: [
      { id: 'lbl-1', name: 'Backend', color: '#61bd4f' },
    ],
    checklists: [
      {
        id: 'chk-1',
        title: 'Tasks',
        position: 0,
        items: [
          { id: 'item-1', text: 'Create tables', isChecked: true, position: 0 },
          { id: 'item-2', text: 'Seed data', isChecked: false, position: 1 },
        ],
      },
    ],
    comments: [
      {
        id: 'com-1',
        authorId: 'user-1',
        authorName: 'Alice Dev',
        authorAvatarUrl: null,
        text: 'Schema created',
        createdAt: '2026-09-02T20:05:00Z',
      },
    ],
    attachments: [
      {
        id: 'att-1',
        fileName: 'schema.sql',
        fileUrl: '/uploads/schema.sql',
        contentType: 'text/plain',
        fileSizeBytes: 2048,
        createdAt: '2026-09-02T20:06:00Z',
      },
    ],
  };

  const mockBoard: BoardDetail = {
    id: 'board-1',
    workspaceId: 'ws-1',
    workspaceName: 'Dev Team',
    title: 'Sprint 1',
    backgroundColor: '#0079bf',
    backgroundImageUrl: null,
    isClosed: false,
    currentUserRole: 'Owner',
    lists: [
      {
        id: 'list-1',
        boardId: 'board-1',
        title: 'To Do',
        position: 0,
        isArchived: false,
        cards: [],
      },
    ],
  };

  const mockUser: User = {
    id: 'user-1',
    email: 'alice@example.com',
    fullName: 'Alice Dev',
  };

  let cardServiceSpy: {
    getCard: ReturnType<typeof vi.fn>;
    updateCard: ReturnType<typeof vi.fn>;
    deleteCard: ReturnType<typeof vi.fn>;
    getBoardLabels: ReturnType<typeof vi.fn>;
    createLabel: ReturnType<typeof vi.fn>;
    assignLabel: ReturnType<typeof vi.fn>;
    removeLabel: ReturnType<typeof vi.fn>;
    createChecklist: ReturnType<typeof vi.fn>;
    deleteChecklist: ReturnType<typeof vi.fn>;
    createItem: ReturnType<typeof vi.fn>;
    updateItem: ReturnType<typeof vi.fn>;
    deleteItem: ReturnType<typeof vi.fn>;
    addComment: ReturnType<typeof vi.fn>;
    updateComment: ReturnType<typeof vi.fn>;
    deleteComment: ReturnType<typeof vi.fn>;
    uploadAttachment: ReturnType<typeof vi.fn>;
    deleteAttachment: ReturnType<typeof vi.fn>;
    assignMember: ReturnType<typeof vi.fn>;
    removeMember: ReturnType<typeof vi.fn>;
  };

  let authServiceSpy: {
    currentUser: ReturnType<typeof signal<User | null>>;
  };

  beforeEach(async () => {
    cardServiceSpy = {
      getCard: vi.fn().mockReturnValue(of(mockCard)),
      updateCard: vi.fn(),
      deleteCard: vi.fn().mockReturnValue(of(undefined)),
      getBoardLabels: vi.fn().mockReturnValue(of(mockCard.labels)),
      createLabel: vi.fn(),
      assignLabel: vi.fn().mockReturnValue(of(undefined)),
      removeLabel: vi.fn().mockReturnValue(of(undefined)),
      createChecklist: vi.fn(),
      deleteChecklist: vi.fn().mockReturnValue(of(undefined)),
      createItem: vi.fn(),
      updateItem: vi.fn(),
      deleteItem: vi.fn().mockReturnValue(of(undefined)),
      addComment: vi.fn(),
      updateComment: vi.fn(),
      deleteComment: vi.fn().mockReturnValue(of(undefined)),
      uploadAttachment: vi.fn(),
      deleteAttachment: vi.fn().mockReturnValue(of(undefined)),
      assignMember: vi.fn().mockReturnValue(of(undefined)),
      removeMember: vi.fn().mockReturnValue(of(undefined)),
    };

    authServiceSpy = {
      currentUser: signal<User | null>(mockUser),
    };

    await TestBed.configureTestingModule({
      imports: [CardDetailModalComponent],
      providers: [
        { provide: CardService, useValue: cardServiceSpy },
        { provide: AuthService, useValue: authServiceSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CardDetailModalComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('cardId', 'card-1');
    fixture.componentRef.setInput('board', mockBoard);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load card details on init', () => {
    expect(cardServiceSpy.getCard).toHaveBeenCalledWith('card-1');
    expect(cardServiceSpy.getBoardLabels).toHaveBeenCalledWith('board-1');
    expect(component.card()?.title).toBe('Setup Database');
    expect(component.currentListName()).toBe('To Do');
  });

  it('should toggle isComplete', () => {
    const updated = { ...mockCard, isComplete: true };
    cardServiceSpy.updateCard.mockReturnValue(of(updated));

    component.toggleCompleted();

    expect(cardServiceSpy.updateCard).toHaveBeenCalledWith('card-1', {
      title: 'Setup Database',
      description: 'Initial migration and schema',
      dueDate: '2026-09-15T00:00:00Z',
      isComplete: true,
      coverColor: '#0079bf',
      coverImageUrl: null,
    });
    expect(component.card()?.isComplete).toBe(true);
  });

  it('should edit description', () => {
    const updated = { ...mockCard, description: 'New description text' };
    cardServiceSpy.updateCard.mockReturnValue(of(updated));

    component.startEditDescription();
    expect(component.isEditingDescription()).toBe(true);

    component.descriptionControl.setValue('New description text');
    component.saveDescription();

    expect(cardServiceSpy.updateCard).toHaveBeenCalledWith('card-1', {
      title: 'Setup Database',
      description: 'New description text',
      dueDate: '2026-09-15T00:00:00Z',
      isComplete: false,
      coverColor: '#0079bf',
      coverImageUrl: null,
    });
    expect(component.isEditingDescription()).toBe(false);
  });

  it('should calculate checklist progress', () => {
    const checklist = mockCard.checklists[0];
    const progress = component.getChecklistProgress(checklist);
    expect(progress).toBe(50);
  });

  it('should add comment', () => {
    const newComment = {
      id: 'com-2',
      authorId: 'user-1',
      authorName: 'Alice Dev',
      authorAvatarUrl: null,
      text: 'Adding more tests',
      createdAt: '2026-09-02T20:10:00Z',
    };
    cardServiceSpy.addComment.mockReturnValue(of(newComment));

    component.newCommentText.set('Adding more tests');
    component.onAddComment();

    expect(cardServiceSpy.addComment).toHaveBeenCalledWith('card-1', {
      text: 'Adding more tests',
    });
    expect(component.card()?.comments.length).toBe(2);
    expect(component.newCommentText()).toBe('');
  });

  it('should delete card and emit cardDeleted', () => {
    let deletedId: string | null = null;
    component.cardDeleted.subscribe((id) => {
      deletedId = id;
    });

    component.openDeleteCardModal();
    expect(component.isDeleteConfirmOpen()).toBe(true);

    component.confirmDeleteCard();

    expect(cardServiceSpy.deleteCard).toHaveBeenCalledWith('card-1');
    expect(deletedId).toBe('card-1');
  });
});
