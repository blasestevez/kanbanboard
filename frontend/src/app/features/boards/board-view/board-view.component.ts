import {
  Component,
  HostListener,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormsModule,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import {
  CdkDrag,
  CdkDragDrop,
  CdkDragHandle,
  CdkDropList,
  CdkDropListGroup,
  moveItemInArray,
  transferArrayItem,
} from '@angular/cdk/drag-drop';
import { BoardService } from '../../../core/services/board.service';
import { CardService } from '../../../core/services/card.service';
import { WorkspaceService } from '../../../core/services/workspace.service';
import { ToastService } from '../../../core/services/toast.service';
import {
  BoardColorOption,
  BoardDetail,
  BoardList,
  CardSummary,
} from '../../../core/models/board.model';
import { CardDetail } from '../../../core/models/card.model';
import { WorkspaceMember } from '../../../core/models/workspace.model';
import { CardDetailModalComponent } from '../../cards/card-detail-modal/card-detail-modal.component';

@Component({
  selector: 'app-board-view',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    RouterLink,
    CdkDropListGroup,
    CdkDropList,
    CdkDrag,
    CdkDragHandle,
    CardDetailModalComponent,
  ],
  templateUrl: './board-view.component.html',
  styleUrl: './board-view.component.scss',
})
export class BoardViewComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly boardService = inject(BoardService);
  private readonly cardService = inject(CardService);
  private readonly workspaceService = inject(WorkspaceService);
  private readonly toastService = inject(ToastService);

  readonly board = this.boardService.activeBoard;
  readonly isLoading = signal<boolean>(true);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  // Card detail modal
  readonly selectedCardId = signal<string | null>(null);
  readonly workspaceMembers = signal<WorkspaceMember[]>([]);

  // Board editing
  readonly isEditingTitle = signal<boolean>(false);
  readonly titleForm = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.minLength(1), Validators.maxLength(50)]],
  });

  readonly isColorPickerOpen = signal<boolean>(false);
  readonly isDeleteBoardOpen = signal<boolean>(false);
  readonly isSubmitting = signal<boolean>(false);

  // List editing
  readonly isAddingList = signal<boolean>(false);
  readonly newListTitle = signal<string>('');
  readonly editingListId = signal<string | null>(null);
  readonly editingListTitle = signal<string>('');
  readonly activeListMenuId = signal<string | null>(null);

  // Card addition
  readonly activeAddingCardListId = signal<string | null>(null);
  readonly newCardTitle = signal<string>('');

  readonly boardColors: BoardColorOption[] = [
    { id: 'blue', color: '#0079bf', name: 'Blue' },
    { id: 'green', color: '#519839', name: 'Green' },
    { id: 'orange', color: '#d29034', name: 'Orange' },
    { id: 'red', color: '#b04632', name: 'Red' },
    { id: 'purple', color: '#89609e', name: 'Purple' },
    { id: 'pink', color: '#cd5a91', name: 'Pink' },
    { id: 'teal', color: '#00aecc', name: 'Teal' },
    { id: 'lime', color: '#4bbf6b', name: 'Lime' },
    { id: 'dark', color: '#172b4d', name: 'Dark Slate' },
  ];

  readonly canEdit = computed(() => {
    const role = this.board()?.currentUserRole;
    return role === 'Owner' || role === 'Member';
  });

  @HostListener('window:keydown.escape')
  onEscape(): void {
    if (this.isColorPickerOpen()) {
      this.isColorPickerOpen.set(false);
    } else if (this.activeListMenuId()) {
      this.closeListMenu();
    } else if (this.editingListId()) {
      this.cancelEditList();
    } else if (this.isAddingList()) {
      this.cancelAddList();
    } else if (this.activeAddingCardListId()) {
      this.cancelAddCard();
    } else if (this.isEditingTitle()) {
      this.cancelEditTitle();
    } else if (this.isDeleteBoardOpen()) {
      this.closeDeleteBoardModal();
    }
  }

  @HostListener('window:keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    const target = event.target as HTMLElement;
    const isInput =
      target.tagName === 'INPUT' ||
      target.tagName === 'TEXTAREA' ||
      target.tagName === 'SELECT' ||
      target.isContentEditable;

    if (!isInput && (event.key === 'n' || event.key === 'N') && this.canEdit()) {
      if (!this.selectedCardId() && !this.isAddingList()) {
        event.preventDefault();
        this.startAddList();
      }
    }
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadBoard(id);
    } else {
      this.router.navigate(['/workspaces']);
    }
  }

  loadBoard(id: string): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.boardService.getBoard(id).subscribe({
      next: (b) => {
        this.isLoading.set(false);
        this.titleForm.patchValue({ title: b.title });
        this.loadWorkspaceMembers(b.workspaceId);
      },
      error: (err: HttpErrorResponse) => {
        this.isLoading.set(false);
        this.errorMessage.set(this.extractErrorMessage(err));
      },
    });
  }

  loadWorkspaceMembers(workspaceId: string): void {
    this.workspaceService.getWorkspace(workspaceId).subscribe({
      next: (ws) => this.workspaceMembers.set(ws.members),
      error: () => {},
    });
  }

  // Board Title Editing
  startEditTitle(): void {
    if (!this.canEdit()) return;
    const currentTitle = this.board()?.title || '';
    this.titleForm.patchValue({ title: currentTitle });
    this.isEditingTitle.set(true);
  }

  saveTitle(): void {
    const b = this.board();
    if (!b || this.titleForm.invalid || !this.isEditingTitle()) return;

    const newTitle = this.titleForm.getRawValue().title.trim();
    if (newTitle === b.title) {
      this.isEditingTitle.set(false);
      return;
    }

    this.boardService
      .updateBoard(b.id, {
        title: newTitle,
        backgroundColor: b.backgroundColor,
        backgroundImageUrl: b.backgroundImageUrl,
        isClosed: b.isClosed,
      })
      .subscribe({
        next: () => {
          this.isEditingTitle.set(false);
          this.toastService.success('Board renamed.');
        },
        error: (err: HttpErrorResponse) => {
          this.errorMessage.set(this.extractErrorMessage(err));
        },
      });
  }

  cancelEditTitle(): void {
    this.isEditingTitle.set(false);
  }

  // Color Palette
  toggleColorPicker(): void {
    this.isColorPickerOpen.update((open) => !open);
  }

  changeBackgroundColor(color: string): void {
    const b = this.board();
    if (!b) return;

    this.isColorPickerOpen.set(false);

    this.boardService
      .updateBoard(b.id, {
        title: b.title,
        backgroundColor: color,
        backgroundImageUrl: b.backgroundImageUrl,
        isClosed: b.isClosed,
      })
      .subscribe({
        next: () => {
          this.toastService.success('Board theme updated.');
        },
        error: (err: HttpErrorResponse) => {
          this.errorMessage.set(this.extractErrorMessage(err));
        },
      });
  }

  // Drag & Drop Lists (Horizontal)
  onListDrop(event: CdkDragDrop<BoardList[]>): void {
    const b = this.board();
    if (!b || event.previousIndex === event.currentIndex) return;

    const updatedLists = [...b.lists];
    moveItemInArray(updatedLists, event.previousIndex, event.currentIndex);

    // Optimistic UI update
    this.boardService.setActiveBoardLists(updatedLists);

    const listIds = updatedLists.map((l) => l.id);
    this.boardService.reorderLists(b.id, listIds).subscribe({
      error: () => {
        this.loadBoard(b.id);
        this.toastService.error('Failed to save list positions.');
      },
    });
  }

  // Drag & Drop Cards (Vertical & Across Lists)
  onCardDrop(event: CdkDragDrop<CardSummary[]>, targetList: BoardList): void {
    if (!this.canEdit()) return;

    const b = this.board();
    if (!b) return;

    if (event.previousContainer === event.container) {
      if (event.previousIndex === event.currentIndex) return;
      moveItemInArray(
        event.container.data,
        event.previousIndex,
        event.currentIndex
      );
    } else {
      transferArrayItem(
        event.previousContainer.data,
        event.container.data,
        event.previousIndex,
        event.currentIndex
      );
    }

    const movedCard = event.container.data[event.currentIndex];
    if (!movedCard) return;

    movedCard.listId = targetList.id;

    this.cardService
      .moveCard(movedCard.id, {
        targetListId: targetList.id,
        newPosition: event.currentIndex,
      })
      .subscribe({
        error: () => {
          this.loadBoard(b.id);
          this.toastService.error('Failed to move card.');
        },
      });
  }

  // Create List
  startAddList(): void {
    this.newListTitle.set('');
    this.isAddingList.set(true);
  }

  cancelAddList(): void {
    this.isAddingList.set(false);
    this.newListTitle.set('');
  }

  onAddListSubmit(): void {
    const b = this.board();
    const title = this.newListTitle().trim();
    if (!b || !title || this.isSubmitting()) return;

    this.isSubmitting.set(true);
    this.boardService.createList(b.id, { title }).subscribe({
      next: (created) => {
        this.isSubmitting.set(false);
        this.newListTitle.set('');
        this.isAddingList.set(false);
        this.toastService.success(`List "${created.title}" added.`);
      },
      error: (err: HttpErrorResponse) => {
        this.isSubmitting.set(false);
        this.errorMessage.set(this.extractErrorMessage(err));
      },
    });
  }

  // Edit List Title
  startEditList(list: BoardList): void {
    if (!this.canEdit()) return;
    this.editingListId.set(list.id);
    this.editingListTitle.set(list.title);
    this.activeListMenuId.set(null);
  }

  saveListTitle(list: BoardList): void {
    const newTitle = this.editingListTitle().trim();
    if (!newTitle || newTitle === list.title) {
      this.editingListId.set(null);
      return;
    }

    this.boardService
      .updateList(list.id, {
        title: newTitle,
        isArchived: list.isArchived,
      })
      .subscribe({
        next: () => {
          this.editingListId.set(null);
          this.toastService.success('List renamed.');
        },
        error: (err: HttpErrorResponse) => {
          this.errorMessage.set(this.extractErrorMessage(err));
        },
      });
  }

  cancelEditList(): void {
    this.editingListId.set(null);
  }

  // List Menu
  toggleListMenu(listId: string): void {
    this.activeListMenuId.update((current) =>
      current === listId ? null : listId
    );
  }

  closeListMenu(): void {
    this.activeListMenuId.set(null);
  }

  deleteList(list: BoardList): void {
    this.closeListMenu();
    this.boardService.deleteList(list.id).subscribe({
      next: () => {
        this.toastService.success(`List "${list.title}" deleted.`);
      },
      error: (err: HttpErrorResponse) => {
        this.errorMessage.set(this.extractErrorMessage(err));
      },
    });
  }

  // Add Card
  startAddCard(listId: string): void {
    this.activeAddingCardListId.set(listId);
    this.newCardTitle.set('');
  }

  cancelAddCard(): void {
    this.activeAddingCardListId.set(null);
    this.newCardTitle.set('');
  }

  onAddCardSubmit(list: BoardList): void {
    const title = this.newCardTitle().trim();
    if (!title || this.isSubmitting()) return;

    this.isSubmitting.set(true);
    this.cardService.createCard(list.id, { title }).subscribe({
      next: (created) => {
        this.isSubmitting.set(false);
        this.newCardTitle.set('');
        this.activeAddingCardListId.set(null);

        // Append to local list
        const cardSummary: CardSummary = {
          id: created.id,
          listId: created.listId,
          title: created.title,
          description: created.description,
          position: created.position,
          dueDate: created.dueDate,
          isComplete: created.isComplete,
          coverColor: created.coverColor,
          coverImageUrl: created.coverImageUrl,
          commentsCount: 0,
          checklistItemsTotal: 0,
          checklistItemsChecked: 0,
        };
        list.cards.push(cardSummary);
        this.toastService.success(`Card "${created.title}" created.`);
      },
      error: (err: HttpErrorResponse) => {
        this.isSubmitting.set(false);
        this.errorMessage.set(this.extractErrorMessage(err));
      },
    });
  }

  // Card Detail Modal
  openCardModal(card: CardSummary): void {
    this.selectedCardId.set(card.id);
  }

  closeCardModal(): void {
    this.selectedCardId.set(null);
  }

  onCardUpdated(updatedCard: CardDetail): void {
    const b = this.board();
    if (!b) return;

    for (const list of b.lists) {
      const cardIdx = list.cards.findIndex((c) => c.id === updatedCard.id);
      if (cardIdx !== -1) {
        list.cards[cardIdx] = {
          ...list.cards[cardIdx],
          title: updatedCard.title,
          description: updatedCard.description,
          dueDate: updatedCard.dueDate,
          isComplete: updatedCard.isComplete,
          coverColor: updatedCard.coverColor,
          coverImageUrl: updatedCard.coverImageUrl,
          commentsCount: updatedCard.comments.length,
          checklistItemsTotal: updatedCard.checklists.reduce(
            (acc, chk) => acc + chk.items.length,
            0
          ),
          checklistItemsChecked: updatedCard.checklists.reduce(
            (acc, chk) => acc + chk.items.filter((i) => i.isChecked).length,
            0
          ),
        };
        break;
      }
    }
  }

  onCardDeleted(deletedCardId: string): void {
    const b = this.board();
    if (!b) return;

    for (const list of b.lists) {
      list.cards = list.cards.filter((c) => c.id !== deletedCardId);
    }
    this.closeCardModal();
    this.toastService.success('Card deleted.');
  }

  // Delete Board
  openDeleteBoardModal(): void {
    this.isDeleteBoardOpen.set(true);
  }

  closeDeleteBoardModal(): void {
    this.isDeleteBoardOpen.set(false);
  }

  confirmDeleteBoard(): void {
    const b = this.board();
    if (!b || this.isSubmitting()) return;

    this.isSubmitting.set(true);
    this.boardService.deleteBoard(b.id).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.closeDeleteBoardModal();
        this.toastService.success('Board deleted.');
        this.router.navigate(['/workspaces', b.workspaceId]);
      },
      error: (err: HttpErrorResponse) => {
        this.isSubmitting.set(false);
        this.errorMessage.set(this.extractErrorMessage(err));
      },
    });
  }

  isCardOverdue(card: CardSummary): boolean {
    if (!card.dueDate || card.isComplete) return false;
    return new Date(card.dueDate) < new Date();
  }

  private extractErrorMessage(err: HttpErrorResponse): string {
    if (err.status === 0) {
      return 'Unable to connect to the server. Please check your connection.';
    }
    if (err.error?.title) return err.error.title;
    if (err.error?.message) return err.error.message;
    return 'An unexpected error occurred. Please try again.';
  }
}
