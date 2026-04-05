import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { EnvEntry, EnvProject, EnvService } from '../../../core/services/EnvService';

@Component({
  selector: 'app-env-manager',
  imports: [FormsModule, ButtonModule, DialogModule, InputTextModule, ToastModule],
  templateUrl: './env-manager.html',
  styleUrl: './env-manager.scss',
  providers: [MessageService]
})
export class EnvManager implements OnInit {
  private readonly svc = inject(EnvService);
  private readonly msg = inject(MessageService);

  projects = signal<EnvProject[]>([]);
  selectedProject = signal<EnvProject | null>(null);
  envFiles = signal<string[]>([]);
  activeFile = signal<string | null>(null);
  entries = signal<EnvEntry[]>([]);
  revealedKeys = signal<Set<string>>(new Set());

  loading = signal(false);
  saving = signal(false);
  showAddProject = signal(false);
  showNewFile = signal(false);
  newProject = signal({ name: '', path: '' });
  newFileName = signal('.env');

  ngOnInit() {
    this.svc.getProjects().subscribe(p => this.projects.set(p));
  }

  selectProject(p: EnvProject) {
    this.selectedProject.set(p);
    this.activeFile.set(null);
    this.entries.set([]);
    this.revealedKeys.set(new Set());
    this.svc.getFiles(p.id).subscribe(files => {
      this.envFiles.set(files);
      if (files.length > 0) this.loadFile(files[0]);
    });
  }

  loadFile(name: string) {
    this.activeFile.set(name);
    this.loading.set(true);
    this.revealedKeys.set(new Set());
    this.svc.getEntries(this.selectedProject()!.id, name).subscribe({
      next: entries => { this.entries.set(entries); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  addProject() {
    const p = this.newProject();
    if (!p.name.trim() || !p.path.trim()) return;
    this.svc.addProject(p).subscribe({
      next: created => {
        this.projects.update(list => [...list, created]);
        this.showAddProject.set(false);
        this.newProject.set({ name: '', path: '' });
        this.msg.add({ severity: 'success', summary: 'Added', detail: `Project "${created.name}" added` });
      },
      error: () => this.msg.add({ severity: 'error', summary: 'Error', detail: 'Directory not found or invalid path' })
    });
  }

  deleteProject(id: number) {
    this.svc.deleteProject(id).subscribe(() => {
      this.projects.update(list => list.filter(p => p.id !== id));
      if (this.selectedProject()?.id === id) {
        this.selectedProject.set(null);
        this.envFiles.set([]);
        this.activeFile.set(null);
        this.entries.set([]);
      }
    });
  }

  createFile() {
    const name = this.newFileName().trim();
    if (!name) return;
    const project = this.selectedProject()!;
    this.svc.saveEntries(project.id, name, []).subscribe({
      next: () => {
        if (!this.envFiles().includes(name))
          this.envFiles.update(f => [...f, name].sort());
        this.loadFile(name);
        this.showNewFile.set(false);
        this.newFileName.set('.env');
      },
      error: () => this.msg.add({ severity: 'error', summary: 'Error', detail: 'Invalid filename' })
    });
  }

  addEntry() {
    this.entries.update(list => [...list, { key: '', value: '', isSensitive: false }]);
  }

  removeEntry(index: number) {
    this.entries.update(list => list.filter((_, i) => i !== index));
  }

  updateKey(index: number, value: string) {
    this.entries.update(list => list.map((e, i) => i === index ? { ...e, key: value } : e));
  }

  updateValue(index: number, value: string) {
    this.entries.update(list => list.map((e, i) => i === index ? { ...e, value } : e));
  }

  updateSensitive(index: number, checked: boolean) {
    this.entries.update(list => list.map((e, i) => i === index ? { ...e, isSensitive: checked } : e));
  }

  toggleReveal(key: string) {
    this.revealedKeys.update(set => {
      const next = new Set(set);
      next.has(key) ? next.delete(key) : next.add(key);
      return next;
    });
  }

  isRevealed(key: string) {
    return this.revealedKeys().has(key);
  }

  save() {
    const project = this.selectedProject();
    const file = this.activeFile();
    if (!project || !file) return;
    this.saving.set(true);
    this.svc.saveEntries(project.id, file, this.entries()).subscribe({
      next: () => {
        this.saving.set(false);
        this.msg.add({ severity: 'success', summary: 'Saved', detail: `${file} saved` });
      },
      error: () => {
        this.saving.set(false);
        this.msg.add({ severity: 'error', summary: 'Error', detail: 'Failed to save file' });
      }
    });
  }
}
