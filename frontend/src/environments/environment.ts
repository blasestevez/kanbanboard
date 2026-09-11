const getApiUrl = (): string => {
  if (typeof window !== 'undefined') {
    const custom = (window as any).__API_URL__ || localStorage.getItem('kanbanboard_api_url') || localStorage.getItem('trellochocero_api_url');
    if (custom) return custom.replace(/\/+$/, '');
    if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
      return 'https://localhost:5001/api';
    }
  }
  // Default to relative /api when proxied, or your deployed Railway API backend
  return '/api';
};

export const environment = {
  production: true,
  get apiUrl(): string {
    return getApiUrl();
  },
};

