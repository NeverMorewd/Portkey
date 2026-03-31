import { Component, inject, input, output, OnDestroy, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { environment } from '../../../../environments/environment';

interface LogEntry {
  stream: 'stdout' | 'stderr';
  line: string;
  timestamp: string;
}
@Component({
  selector: 'app-log-viewer',
  imports: [DatePipe],
  templateUrl: './log-viewer.html',
  styleUrl: './log-viewer.scss',
})
export class LogViewer implements OnInit, OnDestroy {
  serviceId = input.required<number>();
  statusChanged = output<{ id: number, status: string }>();
  logs = signal<LogEntry[]>([]);

  private connection: HubConnection | null = null;

  async ngOnInit() {
    this.connection = new HubConnectionBuilder()
      .withUrl(`${environment.apiBaseUrl}/hubs/log`)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Debug)
      .build();

    this.connection.on('ReceiveLog', (entry: LogEntry) => {
      this.logs.update(l => [...l, entry]);
    });
    this.connection.on('ServiceStatusChanged', (data: { id: number, status: string }) => {
      this.statusChanged.emit(data);
    });
    console.log('SignalR connecting...');
    await this.connection.start();
    console.log('SignalR connected, state:', this.connection.state);
    await this.connection.invoke('JoinServiceLog', String(this.serviceId()));
    console.log('Joined group service-', this.serviceId());
  }

  async ngOnDestroy() {
    if (this.connection) {
      await this.connection.invoke('LeaveServiceLog', String(this.serviceId()));
      await this.connection.stop();
    }
  }
}
