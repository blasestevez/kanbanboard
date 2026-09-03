import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { BoardService } from './board.service';
import { environment } from '../../../environments/environment';
import {
  BoardDetail,
  BoardList,
  BoardSummary,
  CreateBoardRequest,
  CreateListRequest,
  UpdateBoardRequest,
  UpdateListRequest,
} from '../models/board.model';

describe('BoardService', () => {
  let service: BoardService;
  let httpMock: HttpTestingController;
  const apiUrl = environment.apiUrl;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        BoardService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(BoardService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should get workspace boards', () => {
    const mockBoards: BoardSummary[] = [
      {
        id: 'board-1',
        workspaceId: 'ws-1',
        title: 'Roadmap Q4',
        backgroundColor: '#0079bf',
        backgroundImageUrl: null,
        isClosed: false,
        position: 0,
        listsCount: 3,
        createdAt: '2026-09-02T20:00:00Z',
      },
    ];

    service.getWorkspaceBoards('ws-1').subscribe((boards) => {
      expect(boards).toEqual(mockBoards);
    });

    const req = httpMock.expectOne(`${apiUrl}/workspaces/ws-1/boards`);
    expect(req.request.method).toBe('GET');
    req.flush(mockBoards);
  });

  it('should get board detail and update activeBoard signal', () => {
    const mockDetail: BoardDetail = {
      id: 'board-1',
      workspaceId: 'ws-1',
      workspaceName: 'Alpha Team',
      title: 'Roadmap Q4',
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

    service.getBoard('board-1').subscribe((res) => {
      expect(res).toEqual(mockDetail);
      expect(service.activeBoard()).toEqual(mockDetail);
      expect(service.isLoading()).toBe(false);
    });

    const req = httpMock.expectOne(`${apiUrl}/boards/board-1`);
    expect(req.request.method).toBe('GET');
    req.flush(mockDetail);
  });

  it('should create a board', () => {
    const createReq: CreateBoardRequest = {
      title: 'New Board',
      backgroundColor: '#519839',
    };
    const mockCreated: BoardSummary = {
      id: 'board-2',
      workspaceId: 'ws-1',
      title: 'New Board',
      backgroundColor: '#519839',
      backgroundImageUrl: null,
      isClosed: false,
      position: 1,
      listsCount: 0,
      createdAt: '2026-09-02T20:00:00Z',
    };

    service.createBoard('ws-1', createReq).subscribe((res) => {
      expect(res).toEqual(mockCreated);
    });

    const req = httpMock.expectOne(`${apiUrl}/workspaces/ws-1/boards`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(createReq);
    req.flush(mockCreated);
  });

  it('should update a board', () => {
    const updateReq: UpdateBoardRequest = {
      title: 'Updated Board Title',
      backgroundColor: '#d29034',
      backgroundImageUrl: null,
      isClosed: false,
    };
    const mockUpdated: BoardDetail = {
      id: 'board-1',
      workspaceId: 'ws-1',
      workspaceName: 'Alpha Team',
      title: 'Updated Board Title',
      backgroundColor: '#d29034',
      backgroundImageUrl: null,
      isClosed: false,
      currentUserRole: 'Owner',
      lists: [],
    };

    service.updateBoard('board-1', updateReq).subscribe((res) => {
      expect(res).toEqual(mockUpdated);
      expect(service.activeBoard()?.title).toBe('Updated Board Title');
    });

    const req = httpMock.expectOne(`${apiUrl}/boards/board-1`);
    expect(req.request.method).toBe('PUT');
    req.flush(mockUpdated);
  });

  it('should delete a board', () => {
    service.deleteBoard('board-1').subscribe();

    const req = httpMock.expectOne(`${apiUrl}/boards/board-1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('should create a list and append to active board lists', () => {
    const mockBoard: BoardDetail = {
      id: 'board-1',
      workspaceId: 'ws-1',
      workspaceName: 'Alpha Team',
      title: 'Roadmap',
      backgroundColor: '#0079bf',
      backgroundImageUrl: null,
      isClosed: false,
      currentUserRole: 'Owner',
      lists: [],
    };

    // Set initial active board
    service.getBoard('board-1').subscribe();
    httpMock.expectOne(`${apiUrl}/boards/board-1`).flush(mockBoard);

    const createListReq: CreateListRequest = { title: 'In Progress' };
    const mockList: BoardList = {
      id: 'list-new',
      boardId: 'board-1',
      title: 'In Progress',
      position: 0,
      isArchived: false,
      cards: [],
    };

    service.createList('board-1', createListReq).subscribe((res) => {
      expect(res).toEqual(mockList);
      expect(service.activeBoard()?.lists.length).toBe(1);
      expect(service.activeBoard()?.lists[0].title).toBe('In Progress');
    });

    const req = httpMock.expectOne(`${apiUrl}/boards/board-1/lists`);
    expect(req.request.method).toBe('POST');
    req.flush(mockList);
  });

  it('should update a list', () => {
    const updateListReq: UpdateListRequest = {
      title: 'Done',
      isArchived: false,
    };
    const mockUpdatedList: BoardList = {
      id: 'list-1',
      boardId: 'board-1',
      title: 'Done',
      position: 0,
      isArchived: false,
      cards: [],
    };

    service.updateList('list-1', updateListReq).subscribe((res) => {
      expect(res).toEqual(mockUpdatedList);
    });

    const req = httpMock.expectOne(`${apiUrl}/lists/list-1`);
    expect(req.request.method).toBe('PUT');
    req.flush(mockUpdatedList);
  });

  it('should reorder lists', () => {
    const listIds = ['list-2', 'list-1', 'list-3'];

    service.reorderLists('board-1', listIds).subscribe();

    const req = httpMock.expectOne(`${apiUrl}/boards/board-1/lists/reorder`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ listIds });
    req.flush(null);
  });

  it('should delete a list', () => {
    service.deleteList('list-1').subscribe();

    const req = httpMock.expectOne(`${apiUrl}/lists/list-1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
