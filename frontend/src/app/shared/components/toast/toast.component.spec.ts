import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { vi } from 'vitest';
import { ToastComponent } from './toast.component';
import { Toast, ToastService } from '../../../core/services/toast.service';

describe('ToastComponent', () => {
  let component: ToastComponent;
  let fixture: ComponentFixture<ToastComponent>;

  const mockToasts: Toast[] = [
    {
      id: 'toast-1',
      message: 'Workspace created',
      type: 'success',
      durationMs: 4000,
    },
    {
      id: 'toast-2',
      message: 'Network error',
      type: 'error',
      durationMs: 5000,
    },
  ];

  let toastServiceSpy: {
    toasts: ReturnType<typeof signal<Toast[]>>;
    remove: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    toastServiceSpy = {
      toasts: signal<Toast[]>(mockToasts),
      remove: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [ToastComponent],
      providers: [{ provide: ToastService, useValue: toastServiceSpy }],
    }).compileComponents();

    fixture = TestBed.createComponent(ToastComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render active toasts', () => {
    expect(component.toasts().length).toBe(2);
    expect(component.getToastClass('success')).toBe('toast-success');
    expect(component.getToastClass('error')).toBe('toast-error');
  });

  it('should remove toast when close button clicked', () => {
    component.remove('toast-1');
    expect(toastServiceSpy.remove).toHaveBeenCalledWith('toast-1');
  });
});
