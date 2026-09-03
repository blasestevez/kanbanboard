import { Component, OnInit, inject, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { WorkspaceService } from '../../core/services/workspace.service';

export interface NavItem {
  label: string;
  route: string;
  icon: string;
  badge?: string;
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss',
})
export class SidebarComponent implements OnInit {
  private readonly workspaceService = inject(WorkspaceService);

  readonly isOpen = input<boolean>(true);
  readonly closeSidebar = output<void>();

  readonly workspaces = this.workspaceService.workspaces;

  readonly navItems: NavItem[] = [
    {
      label: 'All Workspaces',
      route: '/workspaces',
      icon: 'workspace',
    },
    {
      label: 'Boards',
      route: '/boards',
      icon: 'board',
    },
  ];

  ngOnInit(): void {
    if (this.workspaces().length === 0) {
      this.workspaceService.getWorkspaces().subscribe({
        error: () => {
          // Silent fallback for sidebar
        },
      });
    }
  }

  onClose(): void {
    this.closeSidebar.emit();
  }

  getWorkspaceInitials(name: string): string {
    if (!name) return 'W';
    return name
      .split(' ')
      .filter((w) => w.length > 0)
      .map((w) => w[0].toUpperCase())
      .slice(0, 2)
      .join('');
  }
}
