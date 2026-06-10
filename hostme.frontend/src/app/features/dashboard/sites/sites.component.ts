import { Component, signal, inject, OnInit, ElementRef, ViewChild, computed } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors, ReactiveFormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { SiteService } from '../../../core/services/site.service';
import { SiteDto } from '../../../core/models/api.models';

function siteNameValidator(control: AbstractControl): ValidationErrors | null {
  const value: string = control.value ?? '';
  return /^[a-z0-9-]*$/.test(value) ? null : { invalidChars: true };
}

@Component({
  selector: 'app-sites',
  standalone: true,
  imports: [ReactiveFormsModule, DatePipe],
  templateUrl: './sites.component.html',
  styleUrl: './sites.component.css',
})
export class SitesComponent implements OnInit {
  private readonly siteService = inject(SiteService);
  private readonly fb = inject(FormBuilder);

  @ViewChild('fileInput') fileInputRef!: ElementRef<HTMLInputElement>;

  readonly NAME_MAX = 60;

  uploadForm: FormGroup = this.fb.group({
    name: [
      '',
      [
        Validators.required,
        Validators.minLength(2),
        Validators.maxLength(this.NAME_MAX),
        siteNameValidator,
      ],
    ],
  });

  sites = signal<SiteDto[]>([]);
  isLoadingSites = signal(false);
  isUploading = signal(false);
  selectedFile = signal<File | null>(null);
  isDraggingOver = signal(false);
  uploadErrors = signal<string[]>([]);
  uploadSuccess = signal(false);
  loadError = signal<string | null>(null);
  isUploadPanelOpen = signal(true);
  confirmDeleteId = signal<string | null>(null);
  isDeleting = signal(false);
  copiedId = signal<string | null>(null);

  readonly nameControl = this.uploadForm.get('name')!;
  nameLength = signal(0);

  toggleUploadPanel(): void {
    this.isUploadPanelOpen.update((v) => !v);
  }

  ngOnInit(): void {
    this.loadSites();
    this.nameControl.valueChanges.subscribe((val) => {
      this.nameLength.set(val?.length ?? 0);
    });
  }

  loadSites(): void {
    this.isLoadingSites.set(true);
    this.loadError.set(null);
    this.siteService.getSites().subscribe({
      next: (res) => {
        this.isLoadingSites.set(false);
        if (!res.isError && res.data) {
          this.sites.set(res.data);
        }
      },
      error: () => {
        this.isLoadingSites.set(false);
        this.loadError.set('Failed to load sites. Please try again.');
      },
    });
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDraggingOver.set(true);
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    this.isDraggingOver.set(false);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDraggingOver.set(false);
    this.setFile(event.dataTransfer?.files[0] ?? null);
  }

  onFileSelected(event: Event): void {
    this.setFile((event.target as HTMLInputElement).files?.[0] ?? null);
  }

  private setFile(file: File | null): void {
    if (file && !file.name.toLowerCase().endsWith('.zip')) {
      this.uploadErrors.set(['Only .zip files are accepted.']);
      return;
    }
    this.uploadErrors.set([]);
    this.selectedFile.set(file);
  }

  clearFile(): void {
    this.selectedFile.set(null);
    if (this.fileInputRef) this.fileInputRef.nativeElement.value = '';
  }

  triggerFileInput(): void {
    this.fileInputRef.nativeElement.click();
  }

  onSubmit(): void {
    if (this.uploadForm.invalid || !this.selectedFile() || this.isUploading()) return;
    this.uploadErrors.set([]);
    this.uploadSuccess.set(false);
    this.isUploading.set(true);

    this.siteService.uploadSite(this.nameControl.value.trim(), this.selectedFile()!).subscribe({
      next: (res) => {
        this.isUploading.set(false);
        if (res.isError) {
          this.uploadErrors.set(res.errors);
        } else if (res.data) {
          this.sites.update((list) => [res.data!, ...list]);
          this.uploadForm.reset();
          this.clearFile();
          this.uploadSuccess.set(true);
          this.isUploadPanelOpen.set(false);
          setTimeout(() => this.uploadSuccess.set(false), 4000);
        }
      },
      error: (err: HttpErrorResponse) => {
        this.isUploading.set(false);
        this.uploadErrors.set(
          err.error?.errors?.length ? err.error.errors : ['Upload failed. Please try again.']
        );
      },
    });
  }

  copyUrl(site: SiteDto): void {
    navigator.clipboard.writeText(site.url).then(() => {
      this.copiedId.set(site.id);
      setTimeout(() => this.copiedId.set(null), 2000);
    });
  }

  requestDelete(id: string): void {
    this.confirmDeleteId.set(id);
  }

  cancelDelete(): void {
    this.confirmDeleteId.set(null);
  }

  confirmDelete(): void {
    const id = this.confirmDeleteId();
    if (!id || this.isDeleting()) return;
    this.isDeleting.set(true);

    this.siteService.deleteSite(id).subscribe({
      next: () => {
        this.sites.update((list) => list.filter((s) => s.id !== id));
        this.confirmDeleteId.set(null);
        this.isDeleting.set(false);
      },
      error: () => {
        this.isDeleting.set(false);
        this.confirmDeleteId.set(null);
      },
    });
  }

  formatBytes(bytes: number): string {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return `${parseFloat((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i]}`;
  }
}
