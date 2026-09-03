export interface CardMember {
  userId: string;
  fullName: string;
  email: string;
  avatarUrl?: string | null;
}

export interface CardLabel {
  id: string;
  name: string;
  color: string;
}

export interface ChecklistItem {
  id: string;
  text: string;
  isChecked: boolean;
  position: number;
}

export interface Checklist {
  id: string;
  title: string;
  position: number;
  items: ChecklistItem[];
}

export interface CardComment {
  id: string;
  authorId: string;
  authorName: string;
  authorAvatarUrl?: string | null;
  text: string;
  createdAt: string;
  updatedAt?: string | null;
}

export interface CardAttachment {
  id: string;
  fileName: string;
  fileUrl: string;
  contentType: string;
  fileSizeBytes: number;
  createdAt: string;
}

export interface CardDetail {
  id: string;
  listId: string;
  boardId: string;
  title: string;
  description?: string | null;
  position: number;
  dueDate?: string | null;
  isComplete: boolean;
  coverColor?: string | null;
  coverImageUrl?: string | null;
  createdAt: string;
  members: CardMember[];
  labels: CardLabel[];
  checklists: Checklist[];
  comments: CardComment[];
  attachments: CardAttachment[];
}

export interface CreateCardRequest {
  title: string;
  description?: string | null;
  coverColor?: string | null;
}

export interface UpdateCardRequest {
  title: string;
  description?: string | null;
  dueDate?: string | null;
  isComplete: boolean;
  coverColor?: string | null;
  coverImageUrl?: string | null;
}

export interface MoveCardRequest {
  targetListId: string;
  newPosition: number;
}

export interface CreateLabelRequest {
  name: string;
  color: string;
}

export interface CreateChecklistRequest {
  title: string;
}

export interface CreateChecklistItemRequest {
  text: string;
}

export interface UpdateChecklistItemRequest {
  text: string;
  isChecked: boolean;
}

export interface CreateCommentRequest {
  text: string;
}

export interface UpdateCommentRequest {
  text: string;
}
