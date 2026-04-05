import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ToastModule } from 'primeng/toast';
import { LoadingService } from '../../core/services/LoadingService';

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ToastModule],
  templateUrl: './shell.html',
  styleUrl: './shell.scss'
})
export class Shell {
  readonly loading = inject(LoadingService);
}
