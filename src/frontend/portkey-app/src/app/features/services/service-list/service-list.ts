import { Component, computed, inject, OnInit, signal } from '@angular/core';
import type { EChartsOption } from 'echarts';
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
import { EChartComponent } from '../../../shared/echart/echart';

@Component({
  selector: 'app-service-list',
  imports: [TableModule, ButtonModule, TagModule, DialogModule, InputTextModule,
    FormsModule, ToastModule, LogViewer, EChartComponent],
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
  checkingId = signal<number | null>(null);
  healthResults = signal<Map<number, { healthy: boolean; reason: string; checkedAt: string }>>(new Map());

  runningCount = computed(() => this.services().filter(s => s.status === 'Running').length);
  stoppedCount = computed(() => this.services().filter(s => s.status === 'Stopped').length);
  selectedServiceName = computed(() => {
    const id = this.selectedServiceId();
    return id !== null ? (this.services().find(s => s.id === id)?.name ?? '') : '';
  });

  statusOption = computed((): EChartsOption => {
    const running = this.services().filter(s => s.status === 'Running').length;
    const stopped = this.services().filter(s => s.status === 'Stopped').length;
    const unhealthy = this.services().filter(s => s.status === 'Unhealthy').length;
    return {
      tooltip: { trigger: 'item', formatter: '{b}: {c} ({d}%)' },
      series: [{
        type: 'pie',
        radius: ['42%', '68%'],
        center: ['50%', '50%'],
        data: [
          { name: 'Running', value: running, itemStyle: { color: '#22c55e' } },
          { name: 'Stopped', value: stopped, itemStyle: { color: '#94a3b8' } },
          { name: 'Unhealthy', value: unhealthy, itemStyle: { color: '#ef4444' } },
        ].filter(d => d.value > 0),
        label: { formatter: '{b}\n{c}', fontSize: 11 }
      }]
    };
  });

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

  checkHealth(id: number) {
    this.checkingId.set(id);
    this.serviceManager.checkHealth(id).subscribe({
      next: result => {
        this.checkingId.set(null);
        this.healthResults.update(m => new Map(m).set(id, result));
        if (result.healthy)
          this.messageService.add({ severity: 'success', summary: 'Healthy', detail: result.reason });
        else
          this.messageService.add({ severity: 'warn', summary: 'Unhealthy', detail: result.reason });
      },
      error: () => this.checkingId.set(null)
    });
  }

  healthTooltip(id: number): string {
    const r = this.healthResults().get(id);
    if (!r) return 'Click to check';
    const time = new Date(r.checkedAt).toLocaleTimeString();
    return `${r.healthy ? '✓' : '✗'} ${r.reason} — ${time}`;
  }

  tagSeverity(status: string): 'success' | 'danger' | 'warn' | 'secondary' {
    if (status === 'Running') return 'success';
    if (status === 'Unhealthy') return 'danger';
    return 'secondary';
  }
}
