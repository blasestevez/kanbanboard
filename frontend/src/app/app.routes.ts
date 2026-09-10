import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { guestGuard } from './core/guards/guest.guard';

export const routes: Routes = [
  {
    path: 'auth',
    canActivate: [guestGuard],
    children: [
      {
        path: 'login',
        loadComponent: () =>
          import('./features/auth/login/login.component').then(
            (m) => m.LoginComponent
          ),
      },
      {
        path: 'register',
        loadComponent: () =>
          import('./features/auth/register/register.component').then(
            (m) => m.RegisterComponent
          ),
      },
      {
        path: 'callback',
        loadComponent: () =>
          import('./features/auth/oauth-callback/oauth-callback.component').then(
            (m) => m.OAuthCallbackComponent
          ),
      },
      {
        path: '',
        redirectTo: 'login',
        pathMatch: 'full',
      },
    ],
  },
  {
    path: '',
    loadComponent: () =>
      import('./layout/main-layout/main-layout.component').then(
        (m) => m.MainLayoutComponent
      ),
    canActivate: [authGuard],
    children: [
      {
        path: 'workspaces',
        loadComponent: () =>
          import('./features/workspaces/workspaces.component').then(
            (m) => m.WorkspacesComponent
          ),
      },
      {
        path: 'workspaces/:id',
        loadComponent: () =>
          import('./features/workspaces/workspace-detail/workspace-detail.component').then(
            (m) => m.WorkspaceDetailComponent
          ),
      },
      {
        path: 'boards/:id',
        loadComponent: () =>
          import('./features/boards/board-view/board-view.component').then(
            (m) => m.BoardViewComponent
          ),
      },
      {
        path: 'boards',
        loadComponent: () =>
          import('./features/workspaces/workspaces.component').then(
            (m) => m.WorkspacesComponent
          ),
      },
      {
        path: '',
        redirectTo: 'workspaces',
        pathMatch: 'full',
      },
    ],
  },
  {
    path: '**',
    redirectTo: '',
  },
];
