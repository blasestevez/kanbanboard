export interface CardSummary {
  id: string;
  listId: string;
  title: string;
  description?: string | null;
  position: number;
  dueDate?: string | null;
  isComplete: boolean;
  coverColor?: string | null;
  coverImageUrl?: string | null;
  commentsCount: number;
  checklistItemsTotal: number;
  checklistItemsChecked: number;
}

export interface BoardList {
  id: string;
  boardId: string;
  title: string;
  position: number;
  isArchived: boolean;
  cards: CardSummary[];
}

export interface BoardSummary {
  id: string;
  workspaceId: string;
  title: string;
  backgroundColor?: string | null;
  backgroundImageUrl?: string | null;
  isClosed: boolean;
  position: number;
  listsCount: number;
  createdAt: string;
}

export interface BoardDetail {
  id: string;
  workspaceId: string;
  workspaceName: string;
  title: string;
  backgroundColor?: string | null;
  backgroundImageUrl?: string | null;
  isClosed: boolean;
  currentUserRole: string;
  lists: BoardList[];
}

export interface CreateBoardRequest {
  title: string;
  backgroundColor?: string | null;
  backgroundImageUrl?: string | null;
}

export interface UpdateBoardRequest {
  title: string;
  backgroundColor?: string | null;
  backgroundImageUrl?: string | null;
  isClosed: boolean;
}

export interface CreateListRequest {
  title: string;
}

export interface UpdateListRequest {
  title: string;
  isArchived: boolean;
}

export interface ReorderListsRequest {
  listIds: string[];
}

export interface BoardColorOption {
  id: string;
  color: string;
  name: string;
}
