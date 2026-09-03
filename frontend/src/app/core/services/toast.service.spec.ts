import { TestBed } from '@angular/core/testing';
import { ToastService } from './toast.service';

describe('ToastService', () => {
  let service: ToastService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ToastService],
    });
    service = TestBed.inject(ToastService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should add toast and update toasts signal', () => {
    service.success('Operation succeeded');
    expect(service.toasts().length).toBe(1);
    expect(service.toasts()[0].message).toBe('Operation succeeded');
    expect(service.toasts()[0].type).toBe('success');
  });

  it('should support error, info, warning methods', () => {
    service.error('Failed to load');
    service.info('Update available');
    service.warning('Session expiring');

    expect(service.toasts().length).toBe(3);
    expect(service.toasts()[0].type).toBe('error');
    expect(service.toasts()[1].type).toBe('info');
    expect(service.toasts()[2].type).toBe('warning');
  });

  it('should remove toast by id', () => {
    const id = service.info('Test toast', 0);
    expect(service.toasts().length).toBe(1);

    service.remove(id);
    expect(service.toasts().length).toBe(0);
  });

  it('should clear all toasts', () => {
    service.info('Toast 1', 0);
    service.info('Toast 2', 0);
    expect(service.toasts().length).toBe(2);

    service.clear();
    expect(service.toasts().length).toBe(0);
  });
});
