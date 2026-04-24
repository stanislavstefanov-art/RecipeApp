import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { ToastService } from '../../core/toast.service';

@Component({
  selector: 'app-toast',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './toast.html',
})
export class Toast {
  protected readonly service = inject(ToastService);
}
