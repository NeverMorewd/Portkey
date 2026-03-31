import { Component, inject, OnInit, signal } from '@angular/core';
import { ServiceEntry, ServiceManager } from '../../../core/services/Servicemanager';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { FormsModule } from '@angular/forms';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { LogViewer } from '../../logs/log-viewer/log-viewer';

@Component({
  selector: 'app-service-list',
  imports: [TableModule,
    ButtonModule,
    TagModule,
    DialogModule,
    InputTextModule,
    FormsModule,
    ToastModule,
    LogViewer],
  templateUrl: './service-list.html',
  styleUrl: './service-list.scss',
  providers: [MessageService]
})
export class ServiceList implements OnInit {
  private readonly serviceManager = inject(ServiceManager);
  private readonly messageService = inject(MessageService);
  services = signal<ServiceEntry[]>([]);
  loading = signal(true);
  showAddDialog = signal(false);
  newService = signal<Partial<ServiceEntry>>({});
  selectedServiceId = signal<number | null>(null);

  ngOnInit() {
    this.serviceManager.getServices().subscribe(services => {
      this.services.set(services);
      this.loading.set(false);
    });
  }
  addService(service: Partial<ServiceEntry>) {
    this.serviceManager.addService(service as ServiceEntry).subscribe(newService => {
      this.services.set([...this.services(), newService]);
      this.showAddDialog.set(false);
      this.newService.set({});
    });
  }
  deleteService(id: number) {
    this.serviceManager.deleteService(id).subscribe(success => {
      if (success) {
        this.services.set(this.services().filter(s => s.id !== id));
      }
    });
  }
  startService(id: number) {
    this.serviceManager.startService(id).subscribe(success => {
      if (success) {
        this.services.update(list =>
          list.map(s => s.id === id ? { ...s, status: 'Running' } : s)
        );
        this.messageService.add({ severity: 'success', summary: 'Started', detail: 'Service started successfully' });
      } else {
        //console.error(`Failed to start service with id ${id}`);
        this.messageService.add({ severity: 'error', summary: 'Failed', detail: 'Failed to start service' });
      }
    });
  }

  stopService(id: number) {
    this.serviceManager.stopService(id).subscribe(success => {
      if (success) {
        this.services.update(list =>
          list.map(s => s.id === id ? { ...s, status: 'Stopped' } : s)
        );
        this.messageService.add({ severity: 'success', summary: 'Stopped', detail: 'Service stopped successfully' });
      } else {
        this.messageService.add({ severity: 'error', summary: 'Failed', detail: 'Failed to stop service' });
      }
    });
  }

  onServiceStatusChanged(data: { id: number, status: string }) {
    this.services.update(list =>
      list.map(s => s.id === data.id ? { ...s, status: data.status as 'Running' | 'Stopped' | 'Unhealthy' } : s)
    );
  }

}
