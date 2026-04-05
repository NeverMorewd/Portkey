import { Component, computed, inject, OnInit, signal } from '@angular/core';
import type { EChartsOption } from 'echarts';
import { PortInfo, PortService } from '../../../core/services/PortService';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ButtonModule } from 'primeng/button';
import { EChartComponent } from '../../../shared/echart/echart';

@Component({
  selector: 'app-port-list',
  imports: [TableModule, TagModule, ButtonModule, EChartComponent],
  templateUrl: './port-list.html',
  styleUrl: './port-list.scss',
})
export class PortList implements OnInit {
  private readonly portService = inject(PortService);
  ports = signal<PortInfo[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  portsByProcess = computed(() => {
    const counts = new Map<string, number>();
    for (const p of this.ports()) {
      const name = p.processName || 'Unknown';
      counts.set(name, (counts.get(name) ?? 0) + 1);
    }
    return Array.from(counts.entries())
      .sort((a, b) => b[1] - a[1])
      .slice(0, 12)
      .map(([name, value]) => ({ name, value }));
  });

  pieOption = computed((): EChartsOption => ({
    tooltip: { trigger: 'item', formatter: '{b}: {c} ports ({d}%)' },
    legend: { type: 'scroll', orient: 'vertical', right: 8, top: 'middle', textStyle: { fontSize: 11 } },
    series: [{
      type: 'pie',
      radius: ['42%', '68%'],
      center: ['38%', '50%'],
      data: this.portsByProcess(),
      label: { show: false },
      emphasis: { label: { show: true, fontSize: 13, fontWeight: 'bold' } }
    }]
  }));

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
