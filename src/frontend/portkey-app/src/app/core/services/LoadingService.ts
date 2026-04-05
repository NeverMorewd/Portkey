import { computed, Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class LoadingService {
  private _count = signal(0);
  isLoading = computed(() => this._count() > 0);

  start() { this._count.update(n => n + 1); }
  stop()  { this._count.update(n => Math.max(0, n - 1)); }
}
