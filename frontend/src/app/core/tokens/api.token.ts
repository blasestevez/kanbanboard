import { InjectionToken } from '@angular/core';

export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => {
    // 1. If user set an override in localStorage
    const saved = localStorage.getItem('kanbanboard_api_url') || localStorage.getItem('trellochocero_api_url');
    if (saved) return saved.replace(/\/+$/, '');

    // 2. If running on localhost
    if (typeof window !== 'undefined' && (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1')) {
      return 'https://localhost:5001/api';
    }

    // 3. Fallback or window global if injected
    const customWindow = typeof window !== 'undefined' ? (window as any) : {};
    if (customWindow.__API_URL__) {
      return customWindow.__API_URL__.replace(/\/+$/, '');
    }

    // Default production API endpoint
    return 'https://trellochocero-production.up.railway.app/api';
  },
});
