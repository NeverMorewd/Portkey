import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { GitRepoInfo, GitRoot, GitService } from '../../../core/services/GitService';

@Component({
  selector: 'app-git-overview',
  imports: [DatePipe, FormsModule, ButtonModule, DialogModule, InputTextModule, ToastModule],
  templateUrl: './git-overview.html',
  styleUrl: './git-overview.scss',
  providers: [MessageService]
})
export class GitOverview implements OnInit {
  private readonly svc = inject(GitService);
  private readonly msg = inject(MessageService);

  roots = signal<GitRoot[]>([]);
  repos = signal<GitRepoInfo[]>([]);
  loading = signal(false);
  refreshingPath = signal<string | null>(null);
  showAddRoot = signal(false);
  newRootPath = signal('');
  filterText = signal('');

  filteredRepos = computed(() => {
    const q = this.filterText().toLowerCase();
    return q
      ? this.repos().filter(r => r.name.toLowerCase().includes(q) || r.branch.toLowerCase().includes(q))
      : this.repos();
  });

  cleanCount  = computed(() => this.repos().filter(r => !r.isDirty && r.untrackedFiles === 0).length);
  dirtyCount  = computed(() => this.repos().filter(r => r.isDirty || r.untrackedFiles > 0).length);

  ngOnInit() {
    this.svc.getRoots().subscribe(roots => {
      this.roots.set(roots);
      if (roots.length > 0) this.scan();
    });
  }

  scan() {
    this.loading.set(true);
    this.svc.getRepos().subscribe({
      next: repos => { this.repos.set(repos); this.loading.set(false); },
      error: () => { this.loading.set(false); }
    });
  }

  addRoot() {
    const path = this.newRootPath().trim();
    if (!path) return;
    this.svc.addRoot(path).subscribe({
      next: root => {
        this.roots.update(list => [...list, root]);
        this.showAddRoot.set(false);
        this.newRootPath.set('');
        this.scan();
      },
      error: () => this.msg.add({ severity: 'error', summary: 'Error', detail: 'Directory not found' })
    });
  }

  deleteRoot(id: number) {
    this.svc.deleteRoot(id).subscribe(() => {
      this.roots.update(list => list.filter(r => r.id !== id));
      this.scan();
    });
  }

  refreshRepo(repo: GitRepoInfo) {
    this.refreshingPath.set(repo.path);
    this.svc.refreshRepo(repo.path).subscribe({
      next: updated => {
        this.repos.update(list => list.map(r => r.path === updated.path ? updated : r));
        this.refreshingPath.set(null);
      },
      error: () => this.refreshingPath.set(null)
    });
  }

  statusLabel(repo: GitRepoInfo): string {
    if (repo.isDirty) return 'Modified';
    if (repo.untrackedFiles > 0) return 'Untracked';
    return 'Clean';
  }

  statusClass(repo: GitRepoInfo): string {
    if (repo.isDirty) return 'status-modified';
    if (repo.untrackedFiles > 0) return 'status-untracked';
    return 'status-clean';
  }

  relativeDate(dateStr: string | null): string {
    if (!dateStr) return '';
    const d = new Date(dateStr);
    const diff = Date.now() - d.getTime();
    const mins = Math.floor(diff / 60000);
    if (mins < 1) return 'just now';
    if (mins < 60) return `${mins}m ago`;
    const hrs = Math.floor(mins / 60);
    if (hrs < 24) return `${hrs}h ago`;
    const days = Math.floor(hrs / 24);
    if (days < 30) return `${days}d ago`;
    return new Date(dateStr).toLocaleDateString();
  }
}
