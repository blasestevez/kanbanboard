import {
  Component,
  ElementRef,
  HostListener,
  computed,
  inject,
  output,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.scss',
})
export class NavbarComponent {
  private readonly authService = inject(AuthService);
  private readonly elementRef = inject(ElementRef);

  readonly toggleSidebar = output<void>();

  readonly isDropdownOpen = signal(false);
  readonly currentUser = this.authService.currentUser;

  readonly userInitials = computed(() => {
    const user = this.currentUser();
    if (!user || !user.fullName) return 'U';
    return user.fullName
      .split(' ')
      .filter((n) => n.length > 0)
      .map((n) => n[0].toUpperCase())
      .slice(0, 2)
      .join('');
  });

  toggleMenu(): void {
    this.isDropdownOpen.update((open) => !open);
  }

  closeMenu(): void {
    this.isDropdownOpen.set(false);
  }

  onToggleSidebar(): void {
    this.toggleSidebar.emit();
  }

  onLogout(): void {
    this.closeMenu();
    this.authService.logout();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.elementRef.nativeElement.contains(event.target as Node)) {
      this.closeMenu();
    }
  }
}
