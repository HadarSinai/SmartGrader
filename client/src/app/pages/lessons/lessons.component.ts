import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { CalendarModule } from 'primeng/calendar';
import { TagModule } from 'primeng/tag';
import { ChipModule } from 'primeng/chip';
import { ProgressBarModule } from 'primeng/progressbar';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { SkeletonModule } from 'primeng/skeleton';
import { LessonsService } from '@services/lessons.service';
import { LessonResponseDto, CreateLessonRequestDto, UpdateLessonRequestDto } from '@models/lesson.model';

interface LessonExtended extends LessonResponseDto {
  status?: 'Scheduled' | 'In Progress' | 'Completed' | 'Cancelled';
  participantCount?: number;
  completionRate?: number;
  duration?: number;
  difficulty?: 'Beginner' | 'Intermediate' | 'Advanced';
  tags?: string[];
}

@Component({
  selector: 'app-lessons',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    TableModule,
    ButtonModule,
    CardModule,
    DialogModule,
    InputTextModule,
    CalendarModule,
    TagModule,
    ChipModule,
    ProgressBarModule,
    TooltipModule,
    ConfirmDialogModule,
    SkeletonModule
  ],
  providers: [ConfirmationService],
  template: `
    <p-card>
      <ng-template pTemplate="header">
        <div class="card-header">
          <div>
            <h2 class="card-title">Lessons</h2>
            <p class="card-subtitle">Manage your lessons and courses</p>
          </div>
          <p-button
            label="New Lesson"
            icon="pi pi-plus"
            (onClick)="openCreateDialog()">
          </p-button>
        </div>
      </ng-template>

      <p-table
        [value]="lessons"
        [loading]="loading"
        responsiveLayout="scroll"
        [paginator]="true"
        [rows]="10"
        [rowsPerPageOptions]="[10, 25, 50]"
        [globalFilterFields]="['name', 'subject', 'teacherName']"
        styleClass="p-datatable-striped">
        <ng-template pTemplate="caption">
          <div class="table-header">
            <span class="p-input-icon-left">
              <i class="pi pi-search"></i>
              <input pInputText type="text" (input)="onSearch($event)" placeholder="Search lessons..." />
            </span>
          </div>
        </ng-template>
        <ng-template pTemplate="header">
          <tr>
            <th pSortableColumn="name">Lesson <p-sortIcon field="name"></p-sortIcon></th>
            <th pSortableColumn="subject">Subject <p-sortIcon field="subject"></p-sortIcon></th>
            <th pSortableColumn="teacherName">Teacher <p-sortIcon field="teacherName"></p-sortIcon></th>
            <th pSortableColumn="status">Status <p-sortIcon field="status"></p-sortIcon></th>
            <th pSortableColumn="participantCount">Students <p-sortIcon field="participantCount"></p-sortIcon></th>
            <th pSortableColumn="completionRate">Progress <p-sortIcon field="completionRate"></p-sortIcon></th>
            <th>Assignments</th>
            <th style="width: 10rem">Actions</th>
          </tr>
        </ng-template>
        <ng-template pTemplate="body" let-lesson>
          <tr>
            <td>
              <div class="lesson-cell">
                <div>
                  <strong>{{ lesson.name }}</strong>
                  <div class="lesson-meta">
                    <span><i class="pi pi-calendar"></i> {{ lesson.lessonDate | date: 'short' }}</span>
                    <p-chip
                      [label]="lesson.difficulty"
                      [styleClass]="'difficulty-' + lesson.difficulty?.toLowerCase()">
                    </p-chip>
                  </div>
                </div>
              </div>
            </td>
            <td>
              <span class="subject-badge">{{ lesson.subject }}</span>
            </td>
            <td>
              <div class="teacher-cell">
                <i class="pi pi-user"></i>
                <span>{{ lesson.teacherName }}</span>
              </div>
            </td>
            <td>
              <p-tag
                [value]="lesson.status"
                [severity]="getStatusSeverity(lesson.status)">
              </p-tag>
            </td>
            <td>
              <div class="students-cell">
                <i class="pi pi-users"></i>
                <strong>{{ lesson.participantCount }}</strong>
              </div>
            </td>
            <td>
              <div class="progress-cell">
                <span>{{ lesson.completionRate }}%</span>
                <p-progressBar
                  [value]="lesson.completionRate"
                  [showValue]="false"
                  [style]="{'height': '6px', 'margin-top': '4px'}">
                </p-progressBar>
              </div>
            </td>
            <td>
              <p-button
                [label]="lesson.assignmentsCount.toString()"
                icon="pi pi-file"
                [text]="true"
                size="small"
                (onClick)="viewAssignments(lesson.id)">
              </p-button>
            </td>
            <td>
              <div class="action-buttons">
                <p-button
                  icon="pi pi-pencil"
                  [text]="true"
                  [rounded]="true"
                  severity="info"
                  pTooltip="Edit"
                  tooltipPosition="top"
                  (onClick)="openEditDialog(lesson)">
                </p-button>
                <p-button
                  icon="pi pi-trash"
                  [text]="true"
                  [rounded]="true"
                  severity="danger"
                  pTooltip="Delete"
                  tooltipPosition="top"
                  (onClick)="confirmDelete(lesson)">
                </p-button>
              </div>
            </td>
          </tr>
        </ng-template>
        <ng-template pTemplate="emptymessage">
          <tr>
            <td colspan="8">
              <div class="empty-state">
                <i class="pi pi-inbox"></i>
                <p>No lessons found</p>
                <p-button label="Create First Lesson" icon="pi pi-plus" (onClick)="openCreateDialog()"></p-button>
              </div>
            </td>
          </tr>
        </ng-template>
      </p-table>
    </p-card>

    <p-dialog
      [(visible)]="displayDialog"
      [header]="isEditMode ? 'Edit Lesson' : 'Create Lesson'"
      [modal]="true"
      [style]="{width: '500px'}"
      [draggable]="false"
      [resizable]="false">
      <form [formGroup]="form" (ngSubmit)="onSubmit()">
        <div class="form-grid">
          <div class="form-field">
            <label for="name">Name *</label>
            <input pInputText id="name" formControlName="name" placeholder="Enter lesson name" />
            <small class="p-error" *ngIf="form.get('name')?.invalid && form.get('name')?.touched">
              Name is required
            </small>
          </div>

          <div class="form-field">
            <label for="subject">Subject *</label>
            <input pInputText id="subject" formControlName="subject" placeholder="Enter subject" />
            <small class="p-error" *ngIf="form.get('subject')?.invalid && form.get('subject')?.touched">
              Subject is required
            </small>
          </div>

          <div class="form-field">
            <label for="lessonDate">Lesson Date *</label>
            <p-calendar
              inputId="lessonDate"
              formControlName="lessonDate"
              [showTime]="true"
              [showSeconds]="false"
              dateFormat="yy-mm-dd"
              placeholder="Select date and time"
              appendTo="body">
            </p-calendar>
            <small class="p-error" *ngIf="form.get('lessonDate')?.invalid && form.get('lessonDate')?.touched">
              Date is required
            </small>
          </div>

          <div class="form-field">
            <label for="teacherName">Teacher Name *</label>
            <input pInputText id="teacherName" formControlName="teacherName" placeholder="Enter teacher name" />
            <small class="p-error" *ngIf="form.get('teacherName')?.invalid && form.get('teacherName')?.touched">
              Teacher name is required
            </small>
          </div>
        </div>

        <div class="dialog-footer">
          <p-button
            label="Cancel"
            severity="secondary"
            [outlined]="true"
            (onClick)="closeDialog()"
            type="button">
          </p-button>
          <p-button
            [label]="isEditMode ? 'Update' : 'Create'"
            type="submit"
            [loading]="saving"
            [disabled]="form.invalid">
          </p-button>
        </div>
      </form>
    </p-dialog>

    <p-confirmDialog></p-confirmDialog>
  `,
  styles: [`
    .card-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 1.5rem;
    }

    .card-title {
      font-size: 1.5rem;
      font-weight: 700;
      color: var(--app-text-strong);
      margin: 0 0 0.25rem 0;
    }

    .card-subtitle {
      font-size: 0.875rem;
      color: var(--app-muted);
      margin: 0;
    }

    .table-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    :host ::ng-deep .p-input-icon-left > input {
      padding-left: 2.5rem;
    }

    .badge {
      display: inline-block;
      padding: 0.25rem 0.75rem;
      background-color: #e0f2fe;
      color: #0369a1;
      border-radius: 6px;
      font-weight: 600;
      font-size: 0.875rem;
    }

    .action-buttons {
      display: flex;
      gap: 0.5rem;
    }

    .empty-state {
      padding: 3rem 1rem;
      text-align: center;
      color: #94a3b8;
    }

    .empty-state i {
      font-size: 3rem;
      margin-bottom: 1rem;
      display: block;
    }

    .empty-state p {
      margin: 0.5rem 0;
    }

    .form-grid {
      display: grid;
      gap: 1.25rem;
      margin-bottom: 1.5rem;
    }

    .form-field {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .form-field label {
      font-weight: 600;
      color: var(--app-muted);
      font-size: 0.875rem;
    }

    .dialog-footer {
      display: flex;
      gap: 0.75rem;
      justify-content: flex-end;
      margin-top: 1.5rem;
      padding-top: 1.5rem;
      border-top: 1px solid var(--app-border);
    }

    :host ::ng-deep .p-calendar {
      width: 100%;
    }

    :host ::ng-deep .p-error {
      font-size: 0.75rem;
    }

    .lesson-cell {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .lesson-meta {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      font-size: 0.8125rem;
      color: var(--app-muted);
    }

    .lesson-meta i {
      font-size: 0.75rem;
    }

    .subject-badge {
      display: inline-block;
      padding: 0.375rem 0.875rem;
      background: linear-gradient(135deg, #e0f2fe 0%, #bae6fd 100%);
      color: #0369a1;
      border-radius: 6px;
      font-weight: 600;
      font-size: 0.875rem;
    }

    .teacher-cell {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      color: var(--app-text);
    }

    .teacher-cell i {
      color: var(--app-muted);
      font-size: 0.875rem;
    }

    .students-cell {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      color: var(--app-text);
    }

    .students-cell i {
      color: var(--app-muted);
      font-size: 0.875rem;
    }

    .progress-cell {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    .progress-cell span {
      font-size: 0.8125rem;
      font-weight: 600;
      color: var(--app-text);
    }

    :host ::ng-deep .difficulty-beginner {
      background-color: #d1fae5 !important;
      color: #065f46 !important;
    }

    :host ::ng-deep .difficulty-intermediate {
      background-color: #fef3c7 !important;
      color: #92400e !important;
    }

    :host ::ng-deep .difficulty-advanced {
      background-color: #fee2e2 !important;
      color: #991b1b !important;
    }
  `]
})
export class LessonsComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly lessonsService = inject(LessonsService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly messageService = inject(MessageService);
  private readonly router = inject(Router);

  lessons: LessonExtended[] = [];
  loading = false;
  saving = false;
  displayDialog = false;
  isEditMode = false;
  currentLessonId: number | null = null;

  form: FormGroup;

  constructor() {
    this.form = this.fb.group({
      name: ['', Validators.required],
      subject: ['', Validators.required],
      lessonDate: [new Date(), Validators.required],
      teacherName: ['', Validators.required]
    });
  }

  ngOnInit(): void {
    this.loadLessons();
  }

  loadLessons(): void {
    this.loading = true;
    this.lessonsService.getAll().subscribe({
      next: (data: LessonResponseDto[]) => {
        this.lessons = data;
        this.loading = false;
      },
      error: (_error: unknown) => {
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to load lessons' });
        this.loading = false;
      }
    });
  }

  openCreateDialog(): void {
    this.isEditMode = false;
    this.currentLessonId = null;
    this.form.reset({ lessonDate: new Date() });
    this.displayDialog = true;
  }

  openEditDialog(lesson: LessonResponseDto): void {
    this.isEditMode = true;
    this.currentLessonId = lesson.id;
    this.form.patchValue({
      name: lesson.name,
      subject: lesson.subject,
      lessonDate: new Date(lesson.lessonDate),
      teacherName: lesson.teacherName
    });
    this.displayDialog = true;
  }

  closeDialog(): void {
    this.displayDialog = false;
    this.form.reset();
  }

  onSubmit(): void {
    if (this.form.invalid) {
      Object.keys(this.form.controls).forEach(key => {
        this.form.get(key)?.markAsTouched();
      });
      return;
    }

    this.saving = true;
    const formValue = this.form.value;
    const request = {
      name: formValue.name,
      subject: formValue.subject,
      lessonDate: formValue.lessonDate.toISOString(),
      teacherName: formValue.teacherName
    };

    const operation = this.isEditMode
      ? this.lessonsService.update(this.currentLessonId!, request as UpdateLessonRequestDto)
      : this.lessonsService.create(request as CreateLessonRequestDto);

    operation.subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: `Lesson ${this.isEditMode ? 'updated' : 'created'} successfully`
        });
        this.closeDialog();
        this.loadLessons();
        this.saving = false;
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: `Failed to ${this.isEditMode ? 'update' : 'create'} lesson`
        });
        this.saving = false;
      }
    });
  }

  confirmDelete(lesson: LessonResponseDto): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete "${lesson.name}"?`,
      header: 'Confirm Delete',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.deleteLesson(lesson.id);
      }
    });
  }

  deleteLesson(id: number): void {
    this.lessonsService.delete(id).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Lesson deleted successfully' });
        this.loadLessons();
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to delete lesson' });
      }
    });
  }

  onSearch(event: Event): void {
    const inputElement = event.target as HTMLInputElement;
    const value = inputElement.value;
    if (!value) {
      this.loadLessons();
    } else {
      this.lessons = this.lessons.filter(lesson =>
        lesson.name?.toLowerCase().includes(value.toLowerCase()) ||
        lesson.subject?.toLowerCase().includes(value.toLowerCase()) ||
        lesson.teacherName?.toLowerCase().includes(value.toLowerCase())
      );
    }
  }

  getStatusSeverity(status: string | undefined): 'success' | 'info' | 'warning' | 'danger' | 'secondary' | 'contrast' {
    if (status === 'Completed') return 'success';
    if (status === 'In Progress') return 'info';
    if (status === 'Scheduled') return 'warning';
    if (status === 'Cancelled') return 'danger';
    return 'secondary';
  }

  viewAssignments(lessonId: number): void {
    this.router.navigate(['/lessons', lessonId, 'assignments']);
  }
}
