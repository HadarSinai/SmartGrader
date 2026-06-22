import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TagModule } from 'primeng/tag';
import { AvatarModule } from 'primeng/avatar';
import { ProgressBarModule } from 'primeng/progressbar';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { StudentsService } from '@services/students.service';
import { StudentResponseDto, CreateStudentRequestDto, UpdateStudentRequestDto } from '@models/student.model';

interface StudentExtended extends StudentResponseDto {
  email?: string;
  gradeAverage?: number;
  attendanceRate?: number;
  lastActivity?: string;
  status?: 'Active' | 'Inactive' | 'Warning';
  totalPoints?: number;
  rank?: number;
}

@Component({
  selector: 'app-students',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    TableModule,
    ButtonModule,
    CardModule,
    DialogModule,
    InputTextModule,
    TagModule,
    AvatarModule,
    ProgressBarModule,
    TooltipModule,
    ConfirmDialogModule
  ],
  providers: [ConfirmationService],
  template: `
    <p-card>
      <ng-template pTemplate="header">
        <div class="card-header">
          <div>
            <h2 class="card-title">Students</h2>
            <p class="card-subtitle">Manage student profiles and track performance</p>
          </div>
          <div class="header-actions">
            <p-button
              icon="pi pi-download"
              label="Export"
              severity="secondary"
              [outlined]="true"
              (onClick)="exportData()">
            </p-button>
            <p-button
              label="New Student"
              icon="pi pi-plus"
              (onClick)="openCreateDialog()">
            </p-button>
          </div>
        </div>
      </ng-template>

      <p-table
        [value]="students"
        [loading]="loading"
        responsiveLayout="scroll"
        [paginator]="true"
        [rows]="10"
        [rowsPerPageOptions]="[10, 25, 50]"
        [globalFilterFields]="['fullName', 'email', 'className']"
        styleClass="p-datatable-striped"
        [tableStyle]="{'min-width': '60rem'}">
        <ng-template pTemplate="caption">
          <div class="table-header">
            <span class="p-input-icon-left">
              <i class="pi pi-search"></i>
              <input pInputText type="text" (input)="onSearch($event)" placeholder="Search students..." />
            </span>
            <div class="table-stats">
              <span class="stat-item">
                <i class="pi pi-users"></i>
                <strong>{{ students.length }}</strong> Students
              </span>
              <span class="stat-item">
                <i class="pi pi-check-circle" style="color: #16a34a"></i>
                <strong>{{ getActiveCount() }}</strong> Active
              </span>
            </div>
          </div>
        </ng-template>
        <ng-template pTemplate="header">
          <tr>
            <th style="width: 3rem">#</th>
            <th pSortableColumn="fullName">Student <p-sortIcon field="fullName"></p-sortIcon></th>
            <th pSortableColumn="className">Class <p-sortIcon field="className"></p-sortIcon></th>
            <th pSortableColumn="gradeAverage">Grade Avg <p-sortIcon field="gradeAverage"></p-sortIcon></th>
            <th pSortableColumn="attendanceRate">Attendance <p-sortIcon field="attendanceRate"></p-sortIcon></th>
            <th pSortableColumn="submissionsCount">Submissions <p-sortIcon field="submissionsCount"></p-sortIcon></th>
            <th pSortableColumn="totalPoints">Points <p-sortIcon field="totalPoints"></p-sortIcon></th>
            <th>Status</th>
            <th style="width: 10rem">Actions</th>
          </tr>
        </ng-template>
        <ng-template pTemplate="body" let-student let-rowIndex="rowIndex">
          <tr [class.warning-row]="student.status === 'Warning'" [class.inactive-row]="student.status === 'Inactive'">
            <td>
              <span class="rank-badge" *ngIf="student.rank && student.rank <= 3">
                <i class="pi pi-trophy" [style.color]="getRankColor(student.rank)"></i>
              </span>
              <span *ngIf="!student.rank || student.rank > 3">{{ rowIndex + 1 }}</span>
            </td>
            <td>
              <div class="student-cell">
                <p-avatar
                  [label]="getInitials(student.fullName)"
                  shape="circle"
                  [style]="{'background-color': getAvatarColor(student.id), 'color': '#ffffff'}">
                </p-avatar>
                <div class="student-info">
                  <strong>{{ student.fullName }}</strong>
                  <small>{{ student.email }}</small>
                </div>
              </div>
            </td>
            <td>
              <span class="class-badge">{{ student.className }}</span>
            </td>
            <td>
              <div class="grade-cell">
                <strong [style.color]="getGradeColor(student.gradeAverage)">{{ student.gradeAverage }}%</strong>
                <p-progressBar
                  [value]="student.gradeAverage"
                  [showValue]="false"
                  [style]="{'height': '6px', 'margin-top': '4px'}"
                  [styleClass]="getProgressClass(student.gradeAverage)">
                </p-progressBar>
              </div>
            </td>
            <td>
              <div class="attendance-cell">
                <span [style.color]="getAttendanceColor(student.attendanceRate)">
                  {{ student.attendanceRate }}%
                </span>
                <i
                  class="pi pi-info-circle"
                  [pTooltip]="student.attendanceRate + '% attendance rate'"
                  tooltipPosition="top"
                  style="margin-left: 4px; color: #94a3b8; cursor: help;">
                </i>
              </div>
            </td>
            <td>
              <p-button
                [label]="student.submissionsCount.toString()"
                icon="pi pi-file-edit"
                [text]="true"
                size="small"
                (onClick)="viewSubmissions(student.id)">
              </p-button>
            </td>
            <td>
              <div class="points-cell">
                <i class="pi pi-star-fill" style="color: #fbbf24; margin-right: 4px;"></i>
                <strong>{{ student.totalPoints }}</strong>
              </div>
            </td>
            <td>
              <p-tag
                [value]="student.status"
                [severity]="getStatusSeverity(student.status)">
              </p-tag>
            </td>
            <td>
              <div class="action-buttons">
                <p-button
                  icon="pi pi-eye"
                  [text]="true"
                  [rounded]="true"
                  severity="info"
                  pTooltip="View Details"
                  tooltipPosition="top"
                  (onClick)="viewStudent(student.id)">
                </p-button>
                <p-button
                  icon="pi pi-pencil"
                  [text]="true"
                  [rounded]="true"
                  severity="info"
                  pTooltip="Edit"
                  tooltipPosition="top"
                  (onClick)="openEditDialog(student)">
                </p-button>
                <p-button
                  icon="pi pi-trash"
                  [text]="true"
                  [rounded]="true"
                  severity="danger"
                  pTooltip="Delete"
                  tooltipPosition="top"
                  (onClick)="confirmDelete(student)">
                </p-button>
              </div>
            </td>
          </tr>
        </ng-template>
        <ng-template pTemplate="emptymessage">
          <tr>
            <td colspan="9">
              <div class="empty-state">
                <i class="pi pi-users"></i>
                <p>No students found</p>
                <p-button label="Add First Student" icon="pi pi-plus" (onClick)="openCreateDialog()"></p-button>
              </div>
            </td>
          </tr>
        </ng-template>
      </p-table>
    </p-card>

    <p-dialog
      [(visible)]="displayDialog"
      [header]="isEditMode ? 'Edit Student' : 'Create Student'"
      [modal]="true"
      [style]="{width: '500px'}"
      [draggable]="false"
      [resizable]="false">
      <form [formGroup]="form" (ngSubmit)="onSubmit()">
        <div class="form-grid">
          <div class="form-field">
            <label for="fullName">Full Name *</label>
            <input pInputText id="fullName" formControlName="fullName" placeholder="Enter full name" />
            <small class="p-error" *ngIf="form.get('fullName')?.invalid && form.get('fullName')?.touched">
              Full name is required
            </small>
          </div>

          <div class="form-field">
            <label for="className">Class Name *</label>
            <input pInputText id="className" formControlName="className" placeholder="e.g. CS-101" />
            <small class="p-error" *ngIf="form.get('className')?.invalid && form.get('className')?.touched">
              Class name is required
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
      color: #1e293b;
      margin: 0 0 0.25rem 0;
    }

    .card-subtitle {
      font-size: 0.875rem;
      color: #64748b;
      margin: 0;
    }

    .header-actions {
      display: flex;
      gap: 0.75rem;
    }

    .table-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      flex-wrap: wrap;
      gap: 1rem;
    }

    .table-stats {
      display: flex;
      gap: 1.5rem;
    }

    .stat-item {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      color: #64748b;
      font-size: 0.875rem;
    }

    .stat-item i {
      font-size: 1rem;
    }

    .rank-badge {
      font-size: 1.25rem;
    }

    .student-cell {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }

    .student-info {
      display: flex;
      flex-direction: column;
      gap: 0.125rem;
    }

    .student-info strong {
      font-weight: 600;
      color: #1e293b;
    }

    .student-info small {
      color: #64748b;
      font-size: 0.8125rem;
    }

    .class-badge {
      display: inline-block;
      padding: 0.375rem 0.75rem;
      background-color: #f1f5f9;
      color: #475569;
      border-radius: 6px;
      font-weight: 600;
      font-size: 0.8125rem;
    }

    .grade-cell {
      min-width: 100px;
    }

    .attendance-cell {
      display: flex;
      align-items: center;
    }

    .points-cell {
      display: flex;
      align-items: center;
      font-weight: 600;
      color: #1e293b;
    }

    .action-buttons {
      display: flex;
      gap: 0.25rem;
    }

    .warning-row {
      background-color: #fffbeb !important;
    }

    .inactive-row {
      background-color: #f8fafc !important;
      opacity: 0.7;
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
      color: #475569;
      font-size: 0.875rem;
    }

    .dialog-footer {
      display: flex;
      gap: 0.75rem;
      justify-content: flex-end;
      margin-top: 1.5rem;
      padding-top: 1.5rem;
      border-top: 1px solid #e2e8f0;
    }

    :host ::ng-deep .p-progressbar {
      border-radius: 4px;
    }

    :host ::ng-deep .p-progressbar.grade-excellent .p-progressbar-value {
      background: linear-gradient(90deg, #10b981 0%, #059669 100%);
    }

    :host ::ng-deep .p-progressbar.grade-good .p-progressbar-value {
      background: linear-gradient(90deg, #3b82f6 0%, #2563eb 100%);
    }

    :host ::ng-deep .p-progressbar.grade-average .p-progressbar-value {
      background: linear-gradient(90deg, #f59e0b 0%, #d97706 100%);
    }

    :host ::ng-deep .p-progressbar.grade-poor .p-progressbar-value {
      background: linear-gradient(90deg, #ef4444 0%, #dc2626 100%);
    }
  `]
})
export class StudentsComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly studentsService = inject(StudentsService);
  private readonly router = inject(Router);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly messageService = inject(MessageService);

  students: StudentExtended[] = [];
  loading = false;
  saving = false;
  displayDialog = false;
  isEditMode = false;
  currentStudentId: number | null = null;

  form: FormGroup;

  constructor() {
    this.form = this.fb.group({
      fullName: ['', Validators.required],
      className: ['', Validators.required]
    });
  }

  ngOnInit(): void {
    this.loadStudents();
  }

  loadStudents(): void {
    this.loading = true;
    this.studentsService.getAll().subscribe({
      next: (data: StudentResponseDto[]) => {
        this.students = data as unknown as StudentExtended[];
        this.loading = false;
      },
      error: (_error: unknown) => {
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to load students' });
        this.loading = false;
      }
    });
  }

  getInitials(fullName: string | null): string {
    if (!fullName) return '?';
    return fullName.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
  }

  getAvatarColor(id: number): string {
    const colors = ['#3b82f6', '#8b5cf6', '#ec4899', '#f59e0b', '#10b981', '#06b6d4'];
    return colors[id % colors.length];
  }

  getRankColor(rank: number): string {
    if (rank === 1) return '#fbbf24';
    if (rank === 2) return '#94a3b8';
    return '#cd7f32';
  }

  getGradeColor(grade: number | undefined): string {
    if (!grade) return '#64748b';
    if (grade >= 90) return '#10b981';
    if (grade >= 80) return '#3b82f6';
    if (grade >= 70) return '#f59e0b';
    return '#ef4444';
  }

  getAttendanceColor(rate: number | undefined): string {
    if (!rate) return '#64748b';
    if (rate >= 90) return '#10b981';
    if (rate >= 75) return '#f59e0b';
    return '#ef4444';
  }

  getProgressClass(grade: number | undefined): string {
    if (!grade) return '';
    if (grade >= 90) return 'grade-excellent';
    if (grade >= 80) return 'grade-good';
    if (grade >= 70) return 'grade-average';
    return 'grade-poor';
  }

  getStatusSeverity(status: string | undefined): 'success' | 'warning' | 'danger' | 'info' | 'secondary' | 'contrast' {
    if (status === 'Active') return 'success';
    if (status === 'Warning') return 'warning';
    if (status === 'Inactive') return 'danger';
    return 'secondary';
  }

  getActiveCount(): number {
    return this.students.filter(s => s.status === 'Active').length;
  }

  openCreateDialog(): void {
    this.isEditMode = false;
    this.currentStudentId = null;
    this.form.reset();
    this.displayDialog = true;
  }

  openEditDialog(student: StudentResponseDto): void {
    this.isEditMode = true;
    this.currentStudentId = student.id;
    this.form.patchValue({
      fullName: student.fullName,
      className: student.className
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
    const request = this.form.value;

    const operation = this.isEditMode
      ? this.studentsService.update(this.currentStudentId!, request as UpdateStudentRequestDto)
      : this.studentsService.create(request as CreateStudentRequestDto);

    operation.subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: `Student ${this.isEditMode ? 'updated' : 'created'} successfully`
        });
        this.closeDialog();
        this.loadStudents();
        this.saving = false;
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: `Failed to ${this.isEditMode ? 'update' : 'create'} student`
        });
        this.saving = false;
      }
    });
  }

  confirmDelete(student: StudentResponseDto): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete "${student.fullName}"?`,
      header: 'Confirm Delete',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.deleteStudent(student.id);
      }
    });
  }

  deleteStudent(id: number): void {
    this.studentsService.delete(id).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Student deleted successfully' });
        this.loadStudents();
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to delete student' });
      }
    });
  }

  viewStudent(id: number): void {
    this.router.navigate(['/students', id]);
  }

  viewSubmissions(id: number): void {
    this.router.navigate(['/students', id, 'submissions']);
  }

  exportData(): void {
    this.messageService.add({ severity: 'info', summary: 'Export', detail: 'Export functionality coming soon' });
  }

  onSearch(event: Event): void {
    const inputElement = event.target as HTMLInputElement;
    const value = inputElement.value.toLowerCase();
    if (!value) {
      this.loadStudents();
    } else {
      this.students = this.students.filter(student =>
        student.fullName?.toLowerCase().includes(value) ||
        student.email?.toLowerCase().includes(value) ||
        student.className?.toLowerCase().includes(value)
      );
    }
  }
}
