import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { Subject, of } from 'rxjs';
import { vi } from 'vitest';
import { BoardViewComponent } from './board-view.component';
import { BoardService } from '../../../core/services/board.service';
import { CardService } from '../../../core/services/card.service';
import { WorkspaceService } from '../../../core/services/workspace.service';
import { BoardRealtimeService } from '../../../core/services/board-realtime.service';
import { BoardDetail } from '../../../core/models/board.model';
import { CardDetail } from '../../../core/models/card.model';
import { CdkDragDrop } from '@angular/cdk/drag-drop';

describe('BoardViewComponent', () => {
  let component: BoardViewComponent;
  let fixture: ComponentFixture<BoardViewComponent>;

  let mockBoardDetail: BoardDetail;

  let boardServiceSpy: {
    activeBoard: ReturnType<typeof signal<BoardDetail | null>>;
    getBoard: ReturnType<typeof vi.fn>;
    updateBoard: ReturnType<typeof vi.fn>;
    deleteBoard: ReturnType<typeof vi.fn>;
    createList: ReturnType<typeof vi.fn>;
    updateList: ReturnType<typeof vi.fn>;
    reorderLists: ReturnType<typeof vi.fn>;
    deleteList: ReturnType<typeof vi.fn>;
    setActiveBoardLists: ReturnType<typeof vi.fn>;
    updateActiveBoard: ReturnType<typeof vi.fn>;
    addListToActiveBoard: ReturnType<typeof vi.fn>;
    updateListInActiveBoard: ReturnType<typeof vi.fn>;
    removeListFromActiveBoard: ReturnType<typeof vi.fn>;
    reorderListsInActiveBoard: ReturnType<typeof vi.fn>;
    addOrUpdateCardInActiveBoard: ReturnType<typeof vi.fn>;
    moveCardInActiveBoard: ReturnType<typeof vi.fn>;
    removeCardFromActiveBoard: ReturnType<typeof vi.fn>;
  };

  let cardServiceSpy: {
    createCard: ReturnType<typeof vi.fn>;
    moveCard: ReturnType<typeof vi.fn>;
    getCard: ReturnType<typeof vi.fn>;
    getBoardLabels: ReturnType<typeof vi.fn>;
  };

  let workspaceServiceSpy: {
    getWorkspace: ReturnType<typeof vi.fn>;
  };

  let realtimeServiceSpy: {
    isConnected: ReturnType<typeof signal<boolean>>;
    joinBoard: ReturnType<typeof vi.fn>;
    leaveBoard: ReturnType<typeof vi.fn>;
    disconnect: ReturnType<typeof vi.fn>;
    boardUpdated$: Subject<any>;
    listCreated$: Subject<any>;
    listUpdated$: Subject<any>;
    listDeleted$: Subject<any>;
    listsReordered$: Subject<any>;
    cardCreated$: Subject<any>;
    cardUpdated$: Subject<any>;
    cardMoved$: Subject<any>;
    cardDeleted$: Subject<any>;
  };

  beforeEach(async () => {
    mockBoardDetail = {
      id: 'board-1',
      workspaceId: 'ws-1',
      workspaceName: 'Alpha Team',
      title: 'Sprint Board',
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
          cards: [
            {
              id: 'card-1',
              listId: 'list-1',
              title: 'Task 1',
              description: null,
              position: 0,
              dueDate: null,
              isComplete: false,
              coverColor: null,
              coverImageUrl: null,
              commentsCount: 0,
              checklistItemsTotal: 0,
              checklistItemsChecked: 0,
            },
          ],
        },
        {
          id: 'list-2',
          boardId: 'board-1',
          title: 'Done',
          position: 1,
          isArchived: false,
          cards: [],
        },
      ],
    };

    boardServiceSpy = {
      activeBoard: signal<BoardDetail | null>(mockBoardDetail),
      getBoard: vi.fn().mockReturnValue(of(mockBoardDetail)),
      updateBoard: vi.fn(),
      deleteBoard: vi.fn(),
      createList: vi.fn(),
      updateList: vi.fn(),
      reorderLists: vi.fn().mockReturnValue(of(undefined)),
      deleteList: vi.fn(),
      setActiveBoardLists: vi.fn(),
      updateActiveBoard: vi.fn(),
      addListToActiveBoard: vi.fn(),
      updateListInActiveBoard: vi.fn(),
      removeListFromActiveBoard: vi.fn(),
      reorderListsInActiveBoard: vi.fn(),
      addOrUpdateCardInActiveBoard: vi.fn((card) => {
        const targetList = mockBoardDetail.lists.find((l) => l.id === card.listId);
        if (targetList) {
          const idx = targetList.cards.findIndex((c) => c.id === card.id);
          if (idx !== -1) {
            targetList.cards[idx] = { ...targetList.cards[idx], ...card };
          } else {
            targetList.cards.push(card);
          }
        }
      }),
      moveCardInActiveBoard: vi.fn(),
      removeCardFromActiveBoard: vi.fn((cardId) => {
        mockBoardDetail.lists.forEach((l) => {
          l.cards = l.cards.filter((c) => c.id !== cardId);
        });
      }),
    };

    cardServiceSpy = {
      createCard: vi.fn(),
      moveCard: vi.fn().mockReturnValue(of(undefined)),
      getCard: vi.fn(),
      getBoardLabels: vi.fn().mockReturnValue(of([])),
    };

    workspaceServiceSpy = {
      getWorkspace: vi.fn().mockReturnValue(of({ id: 'ws-1', members: [] })),
    };

    realtimeServiceSpy = {
      isConnected: signal<boolean>(true),
      joinBoard: vi.fn().mockResolvedValue(undefined),
      leaveBoard: vi.fn().mockResolvedValue(undefined),
      disconnect: vi.fn().mockResolvedValue(undefined),
      boardUpdated$: new Subject(),
      listCreated$: new Subject(),
      listUpdated$: new Subject(),
      listDeleted$: new Subject(),
      listsReordered$: new Subject(),
      cardCreated$: new Subject(),
      cardUpdated$: new Subject(),
      cardMoved$: new Subject(),
      cardDeleted$: new Subject(),
    };

    await TestBed.configureTestingModule({
      imports: [BoardViewComponent],
      providers: [
        { provide: BoardService, useValue: boardServiceSpy },
        { provide: CardService, useValue: cardServiceSpy },
        { provide: WorkspaceService, useValue: workspaceServiceSpy },
        { provide: BoardRealtimeService, useValue: realtimeServiceSpy },
        provideRouter([
          { path: 'workspaces', component: class {} },
          { path: 'workspaces/:id', component: class {} },
          { path: 'boards/:id', component: class {} },
        ]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: convertToParamMap({ id: 'board-1' }),
            },
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(BoardViewComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load board and join realtime on init', () => {
    expect(boardServiceSpy.getBoard).toHaveBeenCalledWith('board-1');
    expect(realtimeServiceSpy.joinBoard).toHaveBeenCalledWith('board-1');
    expect(component.board()?.title).toBe('Sprint Board');
    expect(component.canEdit()).toBe(true);
    expect(component.isRealtimeConnected()).toBe(true);
  });

  it('should compute connectedLists correctly for CDK drop targets', () => {
    expect(component.connectedLists()).toEqual([
      'cards-list-list-1',
      'cards-list-list-2',
    ]);
  });

  it('should add a card to a list', () => {
    const newCardDetail: CardDetail = {
      id: 'card-new',
      listId: 'list-1',
      boardId: 'board-1',
      title: 'New Card Added',
      description: null,
      position: 1,
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
    cardServiceSpy.createCard.mockReturnValue(of(newCardDetail));

    component.startAddCard('list-1');
    expect(component.activeAddingCardListId()).toBe('list-1');

    component.newCardTitle.set('New Card Added');
    component.onAddCardSubmit(mockBoardDetail.lists[0]);

    expect(cardServiceSpy.createCard).toHaveBeenCalledWith('list-1', {
      title: 'New Card Added',
    });
    expect(boardServiceSpy.addOrUpdateCardInActiveBoard).toHaveBeenCalledWith(newCardDetail);
    expect(mockBoardDetail.lists[0].cards.length).toBe(2);
    expect(component.activeAddingCardListId()).toBeNull();
  });

  it('should open and close card detail modal', () => {
    const card = mockBoardDetail.lists[0].cards[0];
    component.openCardModal(card);
    expect(component.selectedCardId()).toBe('card-1');

    component.closeCardModal();
    expect(component.selectedCardId()).toBeNull();
  });

  it('should update local card when cardUpdated emits', () => {
    const updatedCard: CardDetail = {
      id: 'card-1',
      listId: 'list-1',
      boardId: 'board-1',
      title: 'Task 1 Updated Title',
      description: 'New Desc',
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

    component.onCardUpdated(updatedCard);
    expect(boardServiceSpy.addOrUpdateCardInActiveBoard).toHaveBeenCalledWith(updatedCard);
    expect(mockBoardDetail.lists[0].cards[0].title).toBe('Task 1 Updated Title');
    expect(mockBoardDetail.lists[0].cards[0].isComplete).toBe(true);
  });

  it('should remove local card when cardDeleted emits', () => {
    component.onCardDeleted('card-1');
    expect(boardServiceSpy.removeCardFromActiveBoard).toHaveBeenCalledWith('card-1');
    expect(mockBoardDetail.lists[0].cards.some((c) => c.id === 'card-1')).toBe(false);
  });

  it('should handle card drag and drop between lists', () => {
    const card2 = {
      id: 'card-2',
      listId: 'list-1',
      title: 'Task 2',
      position: 1,
      isComplete: false,
      commentsCount: 0,
      checklistItemsTotal: 0,
      checklistItemsChecked: 0,
    };
    const list1 = mockBoardDetail.lists[0];
    const list2 = mockBoardDetail.lists[1];
    list1.cards = [{ ...mockBoardDetail.lists[0].cards[0], id: 'card-1' }, card2];

    const dropEvent = {
      previousIndex: 0,
      currentIndex: 0,
      previousContainer: { data: list1.cards },
      container: { data: list2.cards },
    } as unknown as CdkDragDrop<any>;

    component.onCardDrop(dropEvent, list2);

    expect(boardServiceSpy.setActiveBoardLists).toHaveBeenCalled();
    expect(cardServiceSpy.moveCard).toHaveBeenCalledWith('card-1', {
      targetListId: 'list-2',
      newPosition: 0,
    });
  });

  it('should react to SignalR events', () => {
    realtimeServiceSpy.cardMoved$.next({
      cardId: 'card-1',
      sourceListId: 'list-1',
      targetListId: 'list-2',
      newPosition: 0,
    });

    expect(boardServiceSpy.moveCardInActiveBoard).toHaveBeenCalledWith(
      'card-1',
      'list-1',
      'list-2',
      0
    );

    realtimeServiceSpy.boardUpdated$.next({
      title: 'Renamed Board',
      isClosed: false,
    });
    expect(boardServiceSpy.updateActiveBoard).toHaveBeenCalledWith({
      title: 'Renamed Board',
      backgroundColor: undefined,
      backgroundImageUrl: undefined,
      isClosed: false,
    });
  });

  it('should leave board and disconnect on destroy', () => {
    component.ngOnDestroy();
    expect(realtimeServiceSpy.leaveBoard).toHaveBeenCalledWith('board-1');
    expect(realtimeServiceSpy.disconnect).toHaveBeenCalled();
  });
});
