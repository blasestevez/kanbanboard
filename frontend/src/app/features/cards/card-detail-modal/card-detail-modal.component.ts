import {
  Component,
  HostListener,
  OnInit,
  computed,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormsModule,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { CardService } from '../../../core/services/card.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import {
  CardAttachment,
  CardComment,
  CardDetail,
  CardLabel,
  Checklist,
  ChecklistItem,
} from '../../../core/models/card.model';
import { BoardDetail } from '../../../core/models/board.model';
import { WorkspaceMember } from '../../../core/models/workspace.model';

type PopoverType = 'members' | 'labels' | 'checklist' | 'dates' | 'cover' | null;

@Component({
  selector: 'app-card-detail-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './card-detail-modal.component.html',
  styleUrl: './card-detail-modal.component.scss',
})
export class CardDetailModalComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly cardService = inject(CardService);
  private readonly authService = inject(AuthService);
  private readonly toastService = inject(ToastService);

  readonly cardId = input.required<string>();
  readonly board = input<BoardDetail | null>(null);
  readonly workspaceMembers = input<WorkspaceMember[]>([]);

  readonly close = output<void>();
  readonly cardUpdated = output<CardDetail>();
  readonly cardDeleted = output<string>();

  readonly currentUser = this.authService.currentUser;
  readonly card = signal<CardDetail | null>(null);
  readonly isLoading = signal<boolean>(true);
  readonly errorMessage = signal<string | null>(null);
  readonly isSubmitting = signal<boolean>(false);

  // Popovers & edit modes
  readonly activePopover = signal<PopoverType>(null);
  readonly isEditingTitle = signal<boolean>(false);
  readonly isEditingDescription = signal<boolean>(false);
  readonly isDeleteConfirmOpen = signal<boolean>(false);

  // Board labels
  readonly boardLabels = signal<CardLabel[]>([]);

  // Inputs for additions
  readonly newChecklistTitle = signal<string>('');
  readonly activeChecklistAddingId = signal<string | null>(null);
  readonly newChecklistItemText = signal<string>('');
  readonly newCommentText = signal<string>('');
  readonly editingCommentId = signal<string | null>(null);
  readonly editingCommentText = signal<string>('');
  readonly newLabelName = signal<string>('');
  readonly newLabelColor = signal<string>('#61bd4f');
  readonly dueDateInput = signal<string>('');

  // Forms
  readonly titleControl = this.fb.nonNullable.control('', [
    Validators.required,
    Validators.minLength(1),
    Validators.maxLength(100),
  ]);
  readonly descriptionControl = this.fb.nonNullable.control('');

  readonly coverColors = [
    '#61bd4f', '#f2d600', '#ff9f1a', '#eb5a46', '#c377e0',
    '#0079bf', '#00c2e0', '#51e898', '#ff78cb', '#344563'
  ];

  readonly labelColors = [
    '#61bd4f', '#f2d600', '#ff9f1a', '#eb5a46', '#c377e0',
    '#0079bf', '#00c2e0', '#51e898', '#ff78cb', '#344563'
  ];

  readonly currentListName = computed(() => {
    const c = this.card();
    const b = this.board();
    if (!c || !b) return 'List';
    return b.lists.find((l) => l.id === c.listId)?.title || 'List';
  });

  readonly isOverdue = computed(() => {
    const c = this.card();
    if (!c?.dueDate || c.isComplete) return false;
    return new Date(c.dueDate) < new Date();
  });

  @HostListener('window:keydown.escape')
  onEscape(): void {
    if (this.activePopover()) {
      this.closePopover();
    } else if (this.isDeleteConfirmOpen()) {
      this.closeDeleteCardModal();
    } else if (this.isEditingTitle()) {
      this.cancelEditTitle();
    } else if (this.isEditingDescription()) {
      this.cancelEditDescription();
    } else if (this.activeChecklistAddingId()) {
      this.cancelAddItemToChecklist();
    } else {
      this.onCloseModal();
    }
  }

  ngOnInit(): void {
    this.loadCard(this.cardId());
    const b = this.board();
    if (b) {
      this.loadBoardLabels(b.id);
    }
  }

  loadCard(id: string): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.cardService.getCard(id).subscribe({
      next: (data) => {
        this.card.set(data);
        this.isLoading.set(false);
        this.titleControl.setValue(data.title);
        this.descriptionControl.setValue(data.description || '');
        if (data.dueDate) {
          const date = new Date(data.dueDate);
          this.dueDateInput.set(date.toISOString().split('T')[0]);
        }
      },
      error: (err: HttpErrorResponse) => {
        this.isLoading.set(false);
        this.errorMessage.set(this.extractErrorMessage(err));
      },
    });
  }

  loadBoardLabels(boardId: string): void {
    this.cardService.getBoardLabels(boardId).subscribe({
      next: (labels) => this.boardLabels.set(labels),
      error: () => {},
    });
  }

  onCloseModal(): void {
    this.close.emit();
  }

  togglePopover(type: PopoverType): void {
    this.activePopover.update((current) => (current === type ? null : type));
  }

  closePopover(): void {
    this.activePopover.set(null);
  }

  // Title Editing
  startEditTitle(): void {
    const c = this.card();
    if (c) {
      this.titleControl.setValue(c.title);
      this.isEditingTitle.set(true);
    }
  }

  saveTitle(): void {
    const c = this.card();
    if (!c || this.titleControl.invalid) return;

    const newTitle = this.titleControl.value.trim();
    if (newTitle === c.title) {
      this.isEditingTitle.set(false);
      return;
    }

    this.cardService
      .updateCard(c.id, {
        title: newTitle,
        description: c.description,
        dueDate: c.dueDate,
        isComplete: c.isComplete,
        coverColor: c.coverColor,
        coverImageUrl: c.coverImageUrl,
      })
      .subscribe({
        next: (updated) => {
          this.card.set(updated);
          this.isEditingTitle.set(false);
          this.cardUpdated.emit(updated);
          this.toastService.success('Card title updated.');
        },
        error: (err: HttpErrorResponse) => {
          this.errorMessage.set(this.extractErrorMessage(err));
        },
      });
  }

  cancelEditTitle(): void {
    this.isEditingTitle.set(false);
  }

  // Description Editing
  startEditDescription(): void {
    const c = this.card();
    if (c) {
      this.descriptionControl.setValue(c.description || '');
      this.isEditingDescription.set(true);
    }
  }

  saveDescription(): void {
    const c = this.card();
    if (!c) return;

    const newDesc = this.descriptionControl.value.trim();

    this.cardService
      .updateCard(c.id, {
        title: c.title,
        description: newDesc || null,
        dueDate: c.dueDate,
        isComplete: c.isComplete,
        coverColor: c.coverColor,
        coverImageUrl: c.coverImageUrl,
      })
      .subscribe({
        next: (updated) => {
          this.card.set(updated);
          this.isEditingDescription.set(false);
          this.cardUpdated.emit(updated);
          this.toastService.success('Description saved.');
        },
        error: (err: HttpErrorResponse) => {
          this.errorMessage.set(this.extractErrorMessage(err));
        },
      });
  }

  cancelEditDescription(): void {
    this.isEditingDescription.set(false);
  }

  // Complete / Incomplete toggle
  toggleCompleted(): void {
    const c = this.card();
    if (!c) return;

    const nextState = !c.isComplete;
    this.cardService
      .updateCard(c.id, {
        title: c.title,
        description: c.description,
        dueDate: c.dueDate,
        isComplete: nextState,
        coverColor: c.coverColor,
        coverImageUrl: c.coverImageUrl,
      })
      .subscribe({
        next: (updated) => {
          this.card.set(updated);
          this.cardUpdated.emit(updated);
          this.toastService.success(nextState ? 'Marked complete!' : 'Marked incomplete.');
        },
        error: (err: HttpErrorResponse) => {
          this.errorMessage.set(this.extractErrorMessage(err));
        },
      });
  }

  // Due Date
  saveDueDate(): void {
    const c = this.card();
    if (!c) return;

    const dateVal = this.dueDateInput();
    const isoDate = dateVal ? new Date(dateVal).toISOString() : null;

    this.cardService
      .updateCard(c.id, {
        title: c.title,
        description: c.description,
        dueDate: isoDate,
        isComplete: c.isComplete,
        coverColor: c.coverColor,
        coverImageUrl: c.coverImageUrl,
      })
      .subscribe({
        next: (updated) => {
          this.card.set(updated);
          this.closePopover();
          this.cardUpdated.emit(updated);
          this.toastService.success('Due date updated.');
        },
        error: (err: HttpErrorResponse) => {
          this.errorMessage.set(this.extractErrorMessage(err));
        },
      });
  }

  removeDueDate(): void {
    this.dueDateInput.set('');
    this.saveDueDate();
  }

  // Cover Color
  setCoverColor(color: string | null): void {
    const c = this.card();
    if (!c) return;

    this.cardService
      .updateCard(c.id, {
        title: c.title,
        description: c.description,
        dueDate: c.dueDate,
        isComplete: c.isComplete,
        coverColor: color,
        coverImageUrl: c.coverImageUrl,
      })
      .subscribe({
        next: (updated) => {
          this.card.set(updated);
          this.closePopover();
          this.cardUpdated.emit(updated);
          this.toastService.success('Cover updated.');
        },
        error: (err: HttpErrorResponse) => {
          this.errorMessage.set(this.extractErrorMessage(err));
        },
      });
  }

  // Member Assignment
  isMemberAssigned(userId: string): boolean {
    return !!this.card()?.members.some((m) => m.userId === userId);
  }

  toggleMember(member: WorkspaceMember): void {
    const c = this.card();
    if (!c) return;

    const assigned = this.isMemberAssigned(member.userId);
    const op$ = assigned
      ? this.cardService.removeMember(c.id, member.userId)
      : this.cardService.assignMember(c.id, member.userId);

    op$.subscribe({
      next: () => {
        this.loadCard(c.id);
        this.toastService.success(assigned ? 'Member removed.' : 'Member assigned.');
      },
      error: (err: HttpErrorResponse) => {
        this.errorMessage.set(this.extractErrorMessage(err));
      },
    });
  }

  // Labels
  isLabelAssigned(labelId: string): boolean {
    return !!this.card()?.labels.some((l) => l.id === labelId);
  }

  toggleLabel(label: CardLabel): void {
    const c = this.card();
    if (!c) return;

    const assigned = this.isLabelAssigned(label.id);
    const op$ = assigned
      ? this.cardService.removeLabel(c.id, label.id)
      : this.cardService.assignLabel(c.id, label.id);

    op$.subscribe({
      next: () => {
        this.loadCard(c.id);
        this.toastService.success(assigned ? 'Label removed.' : 'Label added.');
      },
      error: (err: HttpErrorResponse) => {
        this.errorMessage.set(this.extractErrorMessage(err));
      },
    });
  }

  onCreateLabel(): void {
    const b = this.board();
    const c = this.card();
    const name = this.newLabelName().trim();
    if (!b || !c || !name) return;

    this.cardService
      .createLabel(b.id, {
        name,
        color: this.newLabelColor(),
      })
      .subscribe({
        next: (created) => {
          this.boardLabels.update((labels) => [...labels, created]);
          this.newLabelName.set('');
          this.toggleLabel(created);
        },
        error: (err: HttpErrorResponse) => {
          this.errorMessage.set(this.extractErrorMessage(err));
        },
      });
  }

  // Checklists
  onCreateChecklist(): void {
    const c = this.card();
    const title = this.newChecklistTitle().trim() || 'Checklist';
    if (!c) return;

    this.cardService.createChecklist(c.id, { title }).subscribe({
      next: (checklist) => {
        this.card.update((current) => {
          if (!current) return null;
          return {
            ...current,
            checklists: [...current.checklists, checklist],
          };
        });
        this.newChecklistTitle.set('');
        this.closePopover();
        this.toastService.success(`Checklist "${checklist.title}" created.`);
      },
      error: (err: HttpErrorResponse) => {
        this.errorMessage.set(this.extractErrorMessage(err));
      },
    });
  }

  deleteChecklist(checklistId: string): void {
    const c = this.card();
    if (!c) return;

    this.cardService.deleteChecklist(checklistId).subscribe({
      next: () => {
        this.card.update((current) => {
          if (!current) return null;
          return {
            ...current,
            checklists: current.checklists.filter((chk) => chk.id !== checklistId),
          };
        });
        this.toastService.success('Checklist removed.');
      },
      error: (err: HttpErrorResponse) => {
        this.errorMessage.set(this.extractErrorMessage(err));
      },
    });
  }

  startAddItemToChecklist(checklistId: string): void {
    this.activeChecklistAddingId.set(checklistId);
    this.newChecklistItemText.set('');
  }

  cancelAddItemToChecklist(): void {
    this.activeChecklistAddingId.set(null);
    this.newChecklistItemText.set('');
  }

  onAddChecklistItem(checklistId: string): void {
    const text = this.newChecklistItemText().trim();
    if (!text) return;

    this.cardService.createItem(checklistId, { text }).subscribe({
      next: (item) => {
        this.card.update((current) => {
          if (!current) return null;
          return {
            ...current,
            checklists: current.checklists.map((chk) => {
              if (chk.id === checklistId) {
                return {
                  ...chk,
                  items: [...chk.items, item],
                };
              }
              return chk;
            }),
          };
        });
        this.newChecklistItemText.set('');
      },
      error: (err: HttpErrorResponse) => {
        this.errorMessage.set(this.extractErrorMessage(err));
      },
    });
  }

  toggleChecklistItem(item: ChecklistItem, checklist: Checklist): void {
    const nextChecked = !item.isChecked;
    this.cardService
      .updateItem(item.id, {
        text: item.text,
        isChecked: nextChecked,
      })
      .subscribe({
        next: (updatedItem) => {
          this.card.update((current) => {
            if (!current) return null;
            return {
              ...current,
              checklists: current.checklists.map((chk) => {
                if (chk.id === checklist.id) {
                  return {
                    ...chk,
                    items: chk.items.map((i) =>
                      i.id === item.id ? updatedItem : i
                    ),
                  };
                }
                return chk;
              }),
            };
          });
        },
        error: (err: HttpErrorResponse) => {
          this.errorMessage.set(this.extractErrorMessage(err));
        },
      });
  }

  deleteChecklistItem(itemId: string, checklist: Checklist): void {
    this.cardService.deleteItem(itemId).subscribe({
      next: () => {
        this.card.update((current) => {
          if (!current) return null;
          return {
            ...current,
            checklists: current.checklists.map((chk) => {
              if (chk.id === checklist.id) {
                return {
                  ...chk,
                  items: chk.items.filter((i) => i.id !== itemId),
                };
              }
              return chk;
            }),
          };
        });
      },
      error: (err: HttpErrorResponse) => {
        this.errorMessage.set(this.extractErrorMessage(err));
      },
    });
  }

  getChecklistProgress(checklist: Checklist): number {
    if (!checklist.items || checklist.items.length === 0) return 0;
    const checked = checklist.items.filter((i) => i.isChecked).length;
    return Math.round((checked / checklist.items.length) * 100);
  }

  // Comments
  onAddComment(): void {
    const c = this.card();
    const text = this.newCommentText().trim();
    if (!c || !text) return;

    this.cardService.addComment(c.id, { text }).subscribe({
      next: (comment) => {
        this.card.update((current) => {
          if (!current) return null;
          return {
            ...current,
            comments: [comment, ...current.comments],
          };
        });
        this.newCommentText.set('');
        this.toastService.success('Comment added.');
      },
      error: (err: HttpErrorResponse) => {
        this.errorMessage.set(this.extractErrorMessage(err));
      },
    });
  }

  startEditComment(comment: CardComment): void {
    this.editingCommentId.set(comment.id);
    this.editingCommentText.set(comment.text);
  }

  cancelEditComment(): void {
    this.editingCommentId.set(null);
  }

  saveEditComment(comment: CardComment): void {
    const text = this.editingCommentText().trim();
    if (!text || text === comment.text) {
      this.editingCommentId.set(null);
      return;
    }

    this.cardService.updateComment(comment.id, { text }).subscribe({
      next: (updated) => {
        this.card.update((current) => {
          if (!current) return null;
          return {
            ...current,
            comments: current.comments.map((c) =>
              c.id === comment.id ? updated : c
            ),
          };
        });
        this.editingCommentId.set(null);
        this.toastService.success('Comment updated.');
      },
      error: (err: HttpErrorResponse) => {
        this.errorMessage.set(this.extractErrorMessage(err));
      },
    });
  }

  deleteComment(commentId: string): void {
    this.cardService.deleteComment(commentId).subscribe({
      next: () => {
        this.card.update((current) => {
          if (!current) return null;
          return {
            ...current,
            comments: current.comments.filter((c) => c.id !== commentId),
          };
        });
        this.toastService.success('Comment deleted.');
      },
      error: (err: HttpErrorResponse) => {
        this.errorMessage.set(this.extractErrorMessage(err));
      },
    });
  }

  // Attachments
  onFileUpload(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    const c = this.card();
    if (!file || !c) return;

    this.cardService.uploadAttachment(c.id, file).subscribe({
      next: (attachment) => {
        this.card.update((current) => {
          if (!current) return null;
          return {
            ...current,
            attachments: [attachment, ...current.attachments],
          };
        });
        input.value = '';
        this.toastService.success(`Attachment "${attachment.fileName}" uploaded.`);
      },
      error: (err: HttpErrorResponse) => {
        input.value = '';
        this.errorMessage.set(this.extractErrorMessage(err));
      },
    });
  }

  deleteAttachment(attachmentId: string): void {
    this.cardService.deleteAttachment(attachmentId).subscribe({
      next: () => {
        this.card.update((current) => {
          if (!current) return null;
          return {
            ...current,
            attachments: current.attachments.filter((a) => a.id !== attachmentId),
          };
        });
        this.toastService.success('Attachment deleted.');
      },
      error: (err: HttpErrorResponse) => {
        this.errorMessage.set(this.extractErrorMessage(err));
      },
    });
  }

  // Delete Card
  openDeleteCardModal(): void {
    this.isDeleteConfirmOpen.set(true);
  }

  closeDeleteCardModal(): void {
    this.isDeleteConfirmOpen.set(false);
  }

  confirmDeleteCard(): void {
    const c = this.card();
    if (!c) return;

    this.cardService.deleteCard(c.id).subscribe({
      next: () => {
        this.cardDeleted.emit(c.id);
        this.close.emit();
      },
      error: (err: HttpErrorResponse) => {
        this.errorMessage.set(this.extractErrorMessage(err));
      },
    });
  }

  getInitials(name: string): string {
    if (!name) return 'U';
    return name
      .split(' ')
      .filter((w) => w.length > 0)
      .map((w) => w[0].toUpperCase())
      .slice(0, 2)
      .join('');
  }

  formatBytes(bytes: number): string {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
  }

  private extractErrorMessage(err: HttpErrorResponse): string {
    if (err.status === 0) {
      return 'Unable to connect to server.';
    }
    if (err.error?.title) return err.error.title;
    if (err.error?.message) return err.error.message;
    return 'An unexpected error occurred.';
  }
}
