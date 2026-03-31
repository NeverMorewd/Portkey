import { Routes } from '@angular/router';
import { PortList } from './features/ports/port-list/port-list';
import { ServiceList } from './features/services/service-list/service-list';

  export const routes: Routes = [
    { path: '', redirectTo: 'ports', pathMatch: 'full' },
    { path: 'ports', component: PortList },
    { path: 'services', component: ServiceList },
  ];
