import { Routes } from '@angular/router';
import { Shell } from './layout/shell/shell';
import { PortList } from './features/ports/port-list/port-list';
import { ServiceList } from './features/services/service-list/service-list';
import { SystemMonitor } from './features/system/system-monitor/system-monitor';
import { EnvManager } from './features/env/env-manager/env-manager';
import { GitOverview } from './features/git/git-overview/git-overview';

export const routes: Routes = [
  {
    path: '',
    component: Shell,
    children: [
      { path: '', redirectTo: 'ports', pathMatch: 'full' },
      { path: 'ports', component: PortList },
      { path: 'services', component: ServiceList },
      { path: 'env', component: EnvManager },
      { path: 'git', component: GitOverview },
      { path: 'system', component: SystemMonitor },
    ]
  }
];
