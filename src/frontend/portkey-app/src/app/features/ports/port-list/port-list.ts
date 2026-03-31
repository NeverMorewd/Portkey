import { Component, inject, OnInit, signal } from '@angular/core';
import { PortInfo, PortService } from '../../../core/services/PortService';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ButtonModule } from 'primeng/button';

@Component({
  selector: 'app-port-list',
  imports: [TableModule, TagModule, ButtonModule ],
  templateUrl: './port-list.html',
  styleUrl: './port-list.scss',
})
export class PortList implements OnInit {
  private readonly portService = inject(PortService);
  ports = signal<PortInfo[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  ngOnInit() {
    this.loadPorts();
  }

  refresh() {
    this.loading.set(true);
    this.error.set(null);
    this.loadPorts();
  }

  private loadPorts() {
    this.portService.getPorts().subscribe({
      next: (data) => {
        this.ports.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load ports');
        this.loading.set(false);
      }
    });
  }
}
