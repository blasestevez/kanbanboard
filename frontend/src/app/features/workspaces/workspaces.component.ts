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
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { WorkspaceService } from '../../core/services/workspace.service';
import { ToastService } from '../../core/services/toast.service';
import {
  CreateWorkspaceRequest,
  WorkspaceRole,
  WorkspaceSummary,
} from '../../core/models/workspace.model';

@Component({
  selector: 'app-workspaces',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './workspaces.component.html',
  styleUrl: './workspaces.component.scss',
})
export class WorkspacesComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly workspaceService = inject(WorkspaceService);
  private readonly toastService = inject(ToastService);

  readonly workspaces = this.workspaceService.workspaces;
  readonly isLoading = signal<boolean>(true);
  readonly errorMessage = signal<string | null>(null);
  readonly searchQuery = signal<string>('');

  // Modal State
  readonly isCreateModalOpen = signal<boolean>(false);
  readonly isCreating = signal<boolean>(false);

  readonly createForm = this.fb.nonNullable.group({
    name: [
      '',
      [
        Validators.required,
        Validators.minLength(2),
        Validators.maxLength(50),
      ],
    ],
    description: ['', [Validators.maxLength(250)]],
  });

  readonly filteredWorkspaces = computed(() => {
    const query = this.searchQuery().toLowerCase().trim();
    const list = this.workspaces();
    if (!query) return list;
    return list.filter(
      (w) =>
        w.name.toLowerCase().includes(query) ||
        (w.description && w.description.toLowerCase().includes(query))
    );
  });

  get nameControl() {
    return this.createForm.controls.name;
  }

  get descriptionControl() {
    return this.createForm.controls.description;
  }

  @HostListener('window:keydown.escape')
  onEscape(): void {
    if (this.isCreateModalOpen()) {
      this.closeCreateModal();
    }
  }

  ngOnInit(): void {
    this.loadWorkspaces();
  }

  loadWorkspaces(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.workspaceService.getWorkspaces().subscribe({
      next: () => {
        this.isLoading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.isLoading.set(false);
        this.errorMessage.set(this.extractErrorMessage(err));
      },
    });
  }

  openCreateModal(): void {
    this.createForm.reset();
    this.errorMessage.set(null);
    this.isCreateModalOpen.set(true);
  }

  closeCreateModal(): void {
    this.isCreateModalOpen.set(false);
    this.createForm.reset();
  }

  onCreateSubmit(): void {
    if (this.createForm.invalid || this.isCreating()) {
      this.createForm.markAllAsTouched();
      return;
    }

    this.isCreating.set(true);
    this.errorMessage.set(null);

    const formVal = this.createForm.getRawValue();
    const payload: CreateWorkspaceRequest = {
      name: formVal.name.trim(),
      description: formVal.description?.trim() || null,
    };

    this.workspaceService.createWorkspace(payload).subscribe({
      next: (created) => {
        this.isCreating.set(false);
        this.closeCreateModal();
        this.toastService.success(`Workspace "${created.name}" created!`);
        this.router.navigate(['/workspaces', created.id]);
      },
      error: (err: HttpErrorResponse) => {
        this.isCreating.set(false);
        this.errorMessage.set(this.extractErrorMessage(err));
      },
    });
  }

  onSearch(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.searchQuery.set(input.value);
  }

  onSearchChange(event: Event): void {
    this.onSearch(event);
  }

  clearSearch(): void {
    this.searchQuery.set('');
  }

  getWorkspaceInitials(name: string): string {
    return this.getInitials(name);
  }

  getInitials(name: string): string {
    if (!name) return 'W';
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
