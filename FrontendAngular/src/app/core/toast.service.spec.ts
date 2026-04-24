import { TestBed } from '@angular/core/testing';
import { ToastService } from './toast.service';

describe('ToastService', () => {
  let service: ToastService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ToastService);
  });

  it('should start with an empty toast list', () => {
    expect(service.toasts()).toEqual([]);
  });

  it('adds a toast with the correct kind and message', () => {
    service.show('success', 'Saved');
    const toasts = service.toasts();
    expect(toasts.length).toBe(1);
    expect(toasts[0].kind).toBe('success');
    expect(toasts[0].message).toBe('Saved');
    expect(toasts[0].id).toBeTruthy();
  });

  it('adds toasts for all three kinds', () => {
    service.show('success', 'ok');
    service.show('error', 'fail');
    service.show('info', 'note');
    const kinds = service.toasts().map((t) => t.kind);
    expect(kinds).toEqual(['success', 'error', 'info']);
  });

  it('removes the correct toast on dismiss', () => {
    service.show('success', 'First');
    service.show('error', 'Second');
    const firstId = service.toasts()[0].id;
    service.dismiss(firstId);
    expect(service.toasts().length).toBe(1);
    expect(service.toasts()[0].message).toBe('Second');
  });

  it('dismiss with an unknown id is a no-op', () => {
    service.show('info', 'Present');
    service.dismiss('unknown-id');
    expect(service.toasts().length).toBe(1);
  });

  it('assigns unique ids to each toast', () => {
    service.show('success', 'A');
    service.show('success', 'B');
    const [a, b] = service.toasts();
    expect(a.id).not.toBe(b.id);
  });
});
