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
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { WorkspaceService } from '../../../core/services/workspace.service';
import { BoardService } from '../../../core/services/board.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import {
  WorkspaceDetail,
  WorkspaceMember,
  WorkspaceRole,
} from '../../../core/models/workspace.model';
import { BoardColorOption, BoardSummary } from '../../../core/models/board.model';

type ActiveTab = 'members' | 'boards';

@Component({
  selector: 'app-workspace-detail',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './workspace-detail.component.html',
  styleUrl: './workspace-detail.component.scss',
})
export class WorkspaceDetailComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly workspaceService = inject(WorkspaceService);
  private readonly boardService = inject(BoardService);
  private readonly authService = inject(AuthService);
  private readonly toastService = inject(ToastService);

  readonly workspace = this.workspaceService.activeWorkspace;
  readonly currentUser = this.authService.currentUser;

  readonly isLoading = signal<boolean>(true);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly activeTab = signal<ActiveTab>('members');

  // Boards
  readonly boards = signal<BoardSummary[]>([]);
  readonly isLoadingBoards = signal<boolean>(false);
  readonly isCreateBoardModalOpen = signal<boolean>(false);
  readonly isCreatingBoard = signal<boolean>(false);

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
  readonly selectedBoardColor = signal<string>('#0079bf');

  // Modals state
  readonly isInviteModalOpen = signal<boolean>(false);
  readonly isEditModalOpen = signal<boolean>(false);
  readonly isDeleteConfirmOpen = signal<boolean>(false);
  readonly isLeaveConfirmOpen = signal<boolean>(false);
  readonly memberToRemove = signal<WorkspaceMember | null>(null);

  // Submitting state
  readonly isSubmitting = signal<boolean>(false);

  // Computed properties
  readonly isOwner = computed(() => {
    const ws = this.workspace();
    return ws?.currentUserRole === 'Owner';
  });

  readonly canCreateBoard = computed(() => {
    const role = this.workspace()?.currentUserRole;
    return role === 'Owner' || role === 'Member';
  });

  // Forms
  readonly inviteForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    role: ['Member' as 'Member' | 'Observer', [Validators.required]],
  });

  readonly editForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(50)]],
    description: ['', [Validators.maxLength(250)]],
  });

  readonly createBoardForm = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.minLength(1), Validators.maxLength(50)]],
  });

  get inviteEmailControl() {
    return this.inviteForm.controls.email;
  }

  get editNameControl() {
    return this.editForm.controls.name;
  }

  get boardTitleControl() {
    return this.createBoardForm.controls.title;
  }

  @HostListener('window:keydown.escape')
  onEscape(): void {
    if (this.isCreateBoardModalOpen()) {
      this.closeCreateBoardModal();
    } else if (this.isInviteModalOpen()) {
      this.closeInviteModal();
    } else if (this.isEditModalOpen()) {
      this.closeEditModal();
    } else if (this.isDeleteConfirmOpen()) {
      this.closeDeleteModal();
    } else if (this.isLeaveConfirmOpen()) {
      this.closeLeaveModal();
    } else if (this.memberToRemove()) {
      this.cancelRemoveMember();
    }
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadWorkspace(id);
    } else {
      this.router.navigate(['/workspaces']);
    }
  }

  loadWorkspace(id: string): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.workspaceService.getWorkspace(id).subscribe({
      next: (ws) => {
        this.isLoading.set(false);
        this.editForm.patchValue({
          name: ws.name,
          description: ws.description || '',
        });
        this.loadBoards(ws.id);
      },
      error: (err: HttpErrorResponse) => {
        this.isLoading.set(false);
        this.errorMessage.set(this.extractErrorMessage(err));
      },
    });
  }

  loadBoards(workspaceId: string): void {
    this.isLoadingBoards.set(true);
    this.boardService.getWorkspaceBoards(workspaceId).subscribe({
      next: (boards) => {
        this.boards.set(boards);
        this.isLoadingBoards.set(false);
      },
      error: () => {
        this.isLoadingBoards.set(false);
      },
    });
  }

  setActiveTab(tab: ActiveTab): void {
    this.activeTab.set(tab);
  }

  // Create Board Modal
  openCreateBoardModal(): void {
    this.createBoardForm.reset({ title: '' });
    this.selectedBoardColor.set('#0079bf');
    this.errorMessage.set(null);
    this.isCreateBoardModalOpen.set(true);
  }

  closeCreateBoardModal(): void {
    this.isCreateBoardModalOpen.set(false);
  }

  selectBoardColor(color: string): void {
    this.selectedBoardColor.set(color);
  }

  onCreateBoardSubmit(): void {
    const ws = this.workspace();
    if (!ws || this.createBoardForm.invalid || this.isCreatingBoard()) {
      this.createBoardForm.markAllAsTouched();
      return;
    }

    this.isCreatingBoard.set(true);
    this.errorMessage.set(null);

    const { title } = this.createBoardForm.getRawValue();

    this.boardService
      .createBoard(ws.id, {
        title: title.trim(),
        backgroundColor: this.selectedBoardColor(),
      })
      .subscribe({
        next: (created) => {
          this.isCreatingBoard.set(false);
          this.closeCreateBoardModal();
          this.toastService.success(`Board "${created.title}" created!`);
          this.router.navigate(['/boards', created.id]);
        },
        error: (err: HttpErrorResponse) => {
          this.isCreatingBoard.set(false);
          this.errorMessage.set(this.extractErrorMessage(err));
        },
      });
  }

  // Invite Member Modal
  openInviteModal(): void {
    this.inviteForm.reset({ email: '', role: 'Member' });
    this.errorMessage.set(null);
    this.isInviteModalOpen.set(true);
  }

  closeInviteModal(): void {
    this.isInviteModalOpen.set(false);
  }

  onInviteSubmit(): void {
    const ws = this.workspace();
    if (!ws || this.inviteForm.invalid || this.isSubmitting()) {
      this.inviteForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    const { email, role } = this.inviteForm.getRawValue();

    this.workspaceService
      .addMember(ws.id, {
        email: email.trim(),
        role,
      })
      .subscribe({
        next: (member) => {
          this.isSubmitting.set(false);
          this.closeInviteModal();
          this.toastService.success(`Member ${member.fullName || member.email} added.`);
        },
        error: (err: HttpErrorResponse) => {
          this.isSubmitting.set(false);
          this.errorMessage.set(this.extractErrorMessage(err));
        },
      });
  }

  // Edit Workspace Modal
  openEditModal(): void {
    const ws = this.workspace();
    if (ws) {
      this.editForm.patchValue({
        name: ws.name,
        description: ws.description || '',
      });
    }
    this.errorMessage.set(null);
    this.isEditModalOpen.set(true);
  }

  closeEditModal(): void {
    this.isEditModalOpen.set(false);
  }

  onEditSubmit(): void {
    const ws = this.workspace();
    if (!ws || this.editForm.invalid || this.isSubmitting()) {
      this.editForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    const { name, description } = this.editForm.getRawValue();

    this.workspaceService
      .updateWorkspace(ws.id, {
        name: name.trim(),
        description: description?.trim() || null,
      })
      .subscribe({
        next: () => {
          this.isSubmitting.set(false);
          this.closeEditModal();
          this.toastService.success('Workspace details updated.');
        },
        error: (err: HttpErrorResponse) => {
          this.isSubmitting.set(false);
          this.errorMessage.set(this.extractErrorMessage(err));
        },
      });
  }

  // Change Member Role
  onRoleChange(member: WorkspaceMember, event: Event): void {
    const select = event.target as HTMLSelectElement;
    const newRole = select.value as 'Member' | 'Observer';
    const ws = this.workspace();
    if (!ws) return;

    this.workspaceService
      .updateMemberRole(ws.id, member.userId, { role: newRole })
      .subscribe({
        next: () => {
          this.toastService.success(`Updated role for ${member.fullName} to ${newRole}.`);
        },
        error: (err: HttpErrorResponse) => {
          select.value = member.role;
          this.errorMessage.set(this.extractErrorMessage(err));
        },
      });
  }

  // Remove Member
  promptRemoveMember(member: WorkspaceMember): void {
    this.memberToRemove.set(member);
  }

  cancelRemoveMember(): void {
    this.memberToRemove.set(null);
  }

  confirmRemoveMember(): void {
    const ws = this.workspace();
    const member = this.memberToRemove();
    if (!ws || !member || this.isSubmitting()) return;

    this.isSubmitting.set(true);
    this.workspaceService.removeMember(ws.id, member.userId).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.memberToRemove.set(null);
        this.toastService.success(`${member.fullName} removed from workspace.`);
      },
      error: (err: HttpErrorResponse) => {
        this.isSubmitting.set(false);
        this.errorMessage.set(this.extractErrorMessage(err));
      },
    });
  }

  // Delete Workspace
  openDeleteModal(): void {
    this.isDeleteConfirmOpen.set(true);
  }

  closeDeleteModal(): void {
    this.isDeleteConfirmOpen.set(false);
  }

  confirmDeleteWorkspace(): void {
    const ws = this.workspace();
    if (!ws || this.isSubmitting()) return;

    this.isSubmitting.set(true);
    this.workspaceService.deleteWorkspace(ws.id).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.closeDeleteModal();
        this.toastService.success('Workspace deleted.');
        this.router.navigate(['/workspaces']);
      },
      error: (err: HttpErrorResponse) => {
        this.isSubmitting.set(false);
        this.errorMessage.set(this.extractErrorMessage(err));
      },
    });
  }

  // Leave Workspace
  openLeaveModal(): void {
    this.isLeaveConfirmOpen.set(true);
  }

  closeLeaveModal(): void {
    this.isLeaveConfirmOpen.set(false);
  }

  confirmLeaveWorkspace(): void {
    const ws = this.workspace();
    const user = this.currentUser();
    if (!ws || !user || this.isSubmitting()) return;

    this.isSubmitting.set(true);
    this.workspaceService.removeMember(ws.id, user.id).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.closeLeaveModal();
        this.toastService.success('You left the workspace.');
        this.router.navigate(['/workspaces']);
      },
      error: (err: HttpErrorResponse) => {
        this.isSubmitting.set(false);
        this.errorMessage.set(this.extractErrorMessage(err));
      },
    });
  }

  // Helpers
  getInitials(name: string): string {
    if (!name) return 'U';
    return name
      .split(' ')
      .filter((w) => w.length > 0)
      .map((w) => w[0].toUpperCase())
      .slice(0, 2)
      .join('');
  }

  getRoleBadgeClass(role: WorkspaceRole): string {
    switch (role) {
      case 'Owner':
        return 'badge-owner';
      case 'Member':
        return 'badge-member';
      case 'Observer':
        return 'badge-observer';
      default:
        return '';
    }
  }

  private extractErrorMessage(err: HttpErrorResponse): string {
    if (err.status === 0) {
      return 'Unable to connect to the server. Please check your connection.';
    }
    if (err.error?.errors) {
      const messages = Object.values(err.error.errors).flat() as string[];
      if (messages.length > 0) return messages.join(' ');
    }
    if (err.error?.title) return err.error.title;
    if (err.error?.message) return err.error.message;
    return 'An unexpected error occurred. Please try again.';
  }
}
