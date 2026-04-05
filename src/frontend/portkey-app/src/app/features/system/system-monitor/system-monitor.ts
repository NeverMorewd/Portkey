import { Component, computed, OnDestroy, OnInit, signal } from '@angular/core';
import type { EChartsOption } from 'echarts';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { environment } from '../../../../environments/environment';
import { EChartComponent } from '../../../shared/echart/echart';

interface SystemMetrics {
  cpuPercent: number;
  workingSetMb: number;
  managedMemoryMb: number;
  timestamp: string;
}

const MAX_POINTS = 60;

@Component({
  selector: 'app-system-monitor',
  imports: [EChartComponent],
  templateUrl: './system-monitor.html',
  styleUrl: './system-monitor.scss'
})
export class SystemMonitor implements OnInit, OnDestroy {
  private connection: HubConnection | null = null;

  labels = signal<string[]>([]);
  cpuData = signal<number[]>([]);
  memData = signal<number[]>([]);
  latest = signal<SystemMetrics | null>(null);
  connected = signal(false);

  cpuOption = computed((): EChartsOption => ({
    animation: false,
    grid: { left: 52, right: 16, top: 16, bottom: 28 },
    tooltip: { trigger: 'axis', formatter: (p: any) => `${p[0].value}%` },
    xAxis: { type: 'category', data: this.labels(), axisLabel: { fontSize: 10, interval: 9 } },
    yAxis: { type: 'value', min: 0, max: 100, axisLabel: { formatter: '{value}%', fontSize: 10 } },
    series: [{
      type: 'line',
      data: this.cpuData(),
      smooth: true,
      symbol: 'none',
      lineStyle: { color: '#7c9cff', width: 2 },
      areaStyle: { color: 'rgba(124,156,255,0.15)' }
    }]
  }));

  memOption = computed((): EChartsOption => ({
    animation: false,
    grid: { left: 64, right: 16, top: 16, bottom: 28 },
    tooltip: { trigger: 'axis', formatter: (p: any) => `${p[0].value} MB` },
    xAxis: { type: 'category', data: this.labels(), axisLabel: { fontSize: 10, interval: 9 } },
    yAxis: { type: 'value', min: 0, axisLabel: { formatter: '{value} MB', fontSize: 10 } },
    series: [{
      type: 'line',
      data: this.memData(),
      smooth: true,
      symbol: 'none',
      lineStyle: { color: '#f59e0b', width: 2 },
      areaStyle: { color: 'rgba(245,158,11,0.12)' }
    }]
  }));

  async ngOnInit() {
    this.connection = new HubConnectionBuilder()
      .withUrl(`${environment.apiBaseUrl}/hubs/system`)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    this.connection.on('ReceiveMetrics', (metrics: SystemMetrics) => {
      const time = new Date(metrics.timestamp).toLocaleTimeString();
      this.labels.update(l => [...l.slice(-(MAX_POINTS - 1)), time]);
      this.cpuData.update(d => [...d.slice(-(MAX_POINTS - 1)), metrics.cpuPercent]);
      this.memData.update(d => [...d.slice(-(MAX_POINTS - 1)), metrics.workingSetMb]);
      this.latest.set(metrics);
    });

    this.connection.onreconnecting(() => this.connected.set(false));
    this.connection.onreconnected(() => this.connected.set(true));

    await this.connection.start();
    this.connected.set(true);
  }

  async ngOnDestroy() {
    await this.connection?.stop();
  }
}
