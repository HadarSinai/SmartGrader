import { CommonModule } from "@angular/common";
import { Component, OnInit, inject } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { ActivatedRoute, Router } from "@angular/router";
import { AssignmentExtended } from "@models/assignment-extended.model";
import { AssignmentResponseDto } from "@models/assignment.model";
import { AssignmentsService } from "@services/assignments.service";
import { ConfirmationService, MessageService } from "primeng/api";
import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { ChipModule } from "primeng/chip";
import { ConfirmDialogModule } from "primeng/confirmdialog";
import { DataViewModule } from "primeng/dataview";
import { InputTextModule } from "primeng/inputtext";
import { ProgressBarModule } from "primeng/progressbar";
import { TableModule } from "primeng/table";
import { TagModule } from "primeng/tag";
import { TooltipModule } from "primeng/tooltip";

@Component({
  selector: "app-assignments-list",
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    InputTextModule,
    TableModule,
    ButtonModule,
    CardModule,
    ConfirmDialogModule,
    TagModule,
    ChipModule,
    ProgressBarModule,
    TooltipModule,
    DataViewModule,
  ],
  providers: [ConfirmationService],
  template: `
    <section class="sg-page">
      <div class="pt-3 pb-5">
        <p-card styleClass="sg-card">
          <ng-template pTemplate="header">
            <div
              class="flex flex-column md:flex-row md:align-items-end md:justify-content-between gap-3 px-4 pt-4 pb-2"
            >
              <div class="sg-title">
                <div class="sg-h1">תרגילים</div>
                <div class="sg-h2">ניהול תרגילים לשיעור הנבחר</div>
              </div>

              <div class="flex align-items-center gap-2 flex-wrap">
                <p-button
                  label="חזרה לשיעורים"
                  icon="pi pi-arrow-left"
                  severity="secondary"
                  [outlined]="true"
                  (onClick)="navigateToLessons()"
                >
                </p-button>
                <p-button
                  label="תרגיל חדש"
                  icon="pi pi-plus"
                  styleClass="sg-btn-primary"
                  (onClick)="navigateToCreate()"
                >
                </p-button>
              </div>
            </div>

            <div
              class="flex flex-column md:flex-row md:align-items-center md:justify-content-between gap-3 px-4 pb-3"
            >
              <span class="p-input-icon-right sg-search">
                <i class="pi pi-search" aria-hidden="true"></i>
                <input
                  pInputText
                  type="text"
                  [(ngModel)]="query"
                  placeholder="חיפוש תרגילים..."
                  aria-label="חיפוש תרגילים"
                />
              </span>

              <div class="sg-active-filters" aria-label="מסננים פעילים">
                <p-chip
                  *ngIf="query.trim()"
                  label="שם: {{ query.trim() }}"
                  [removable]="true"
                  (onRemove)="query = ''"
                ></p-chip>
              </div>
            </div>
          </ng-template>

          <!-- Desktop table -->
          <div class="sg-table-wrap desktop-only">
            <p-table
              [value]="filteredAssignments"
              [loading]="loading"
              responsiveLayout="scroll"
              [paginator]="true"
              [rows]="10"
              [rowsPerPageOptions]="[10, 25, 50]"
              styleClass="sg-table"
            >
              <ng-template pTemplate="header">
                <tr>
                  <th pSortableColumn="title" class="text-center">
                    שם <p-sortIcon field="title"></p-sortIcon>
                  </th>
                  <th pSortableColumn="difficulty" class="text-center">
                    רמה <p-sortIcon field="difficulty"></p-sortIcon>
                  </th>
                  <th pSortableColumn="status" class="text-center">
                    סטטוס <p-sortIcon field="status"></p-sortIcon>
                  </th>
                  <th pSortableColumn="dueDate" class="text-center">
                    דדליין <p-sortIcon field="dueDate"></p-sortIcon>
                  </th>
                  <th pSortableColumn="maxScore" class="text-center">
                    נקודות <p-sortIcon field="maxScore"></p-sortIcon>
                  </th>
                  <th pSortableColumn="completionRate" class="text-center">
                    השלמה <p-sortIcon field="completionRate"></p-sortIcon>
                  </th>
                  <th pSortableColumn="averageScore" class="text-center">
                    ממוצע <p-sortIcon field="averageScore"></p-sortIcon>
                  </th>
                  <th class="text-center">בדיקות/הגשות</th>
                  <th class="text-center" style="width: 10rem">פעולות</th>
                </tr>
              </ng-template>

              <ng-template pTemplate="body" let-assignment>
                <tr>
                  <td>
                    <div class="flex flex-column gap-1">
                      <div class="flex align-items-center gap-2 flex-wrap">
                        <div class="font-bold text-color">
                          {{ assignment.title }}
                        </div>
                        <p-chip
                          *ngIf="assignment.isBonus"
                          label="בונוס"
                          icon="pi pi-star"
                          styleClass="sg-bonus-chip"
                        ></p-chip>
                      </div>
                      <div
                        class="text-color-secondary text-sm overflow-hidden text-overflow-ellipsis white-space-nowrap"
                        style="max-width: 320px"
                      >
                        {{ assignment.description }}
                      </div>
                    </div>
                  </td>

                  <td class="text-center">
                    <p-chip
                      *ngIf="assignment.difficulty"
                      [label]="assignment.difficulty"
                      [styleClass]="
                        'difficulty-' + assignment.difficulty?.toLowerCase()
                      "
                    ></p-chip>
                    <span *ngIf="!assignment.difficulty">—</span>
                  </td>

                  <td class="text-center">
                    <p-tag
                      *ngIf="assignment.status"
                      [value]="assignment.status"
                      [severity]="getStatusSeverity(assignment.status)"
                    ></p-tag>
                    <span *ngIf="!assignment.status">—</span>
                  </td>

                  <td class="text-center">
                    <div
                      class="inline-flex align-items-center gap-2 text-color-secondary"
                    >
                      <i class="pi pi-calendar" aria-hidden="true"></i>
                      <span>{{ assignment.dueDate | date: "short" }}</span>
                    </div>
                  </td>

                  <td class="text-center">
                    <div class="inline-flex align-items-center gap-2">
                      <span class="font-bold">{{ assignment.maxScore }}</span>
                      <span class="text-color-secondary text-sm">pts</span>
                      <p-tag
                        *ngIf="assignment.isBonus"
                        [value]="'+' + assignment.bonusValue"
                        severity="success"
                        styleClass="sg-bonus-tag"
                      ></p-tag>
                    </div>
                  </td>

                  <td class="text-center">
                    <div
                      class="flex flex-column gap-1"
                      style="min-width: 120px"
                    >
                      <span class="text-sm font-semibold text-color-secondary"
                        >{{ assignment.completionRate }}%</span
                      >
                      <p-progressBar
                        [value]="assignment.completionRate"
                        [showValue]="false"
                        [style]="{ height: '6px' }"
                      ></p-progressBar>
                    </div>
                  </td>

                  <td class="text-center">
                    <div
                      class="inline-flex align-items-baseline gap-2 font-semibold"
                    >
                      <span
                        [style.color]="getScoreColor(assignment.averageScore)"
                        >{{ assignment.averageScore }}</span
                      >
                      <span class="text-color-secondary text-sm"
                        >/ {{ assignment.maxScore }}</span
                      >
                    </div>
                  </td>

                  <td class="text-center">
                    <div class="flex flex-column gap-2">
                      <span
                        class="inline-flex align-items-center gap-2 text-color-secondary"
                        pTooltip="מקרי בדיקה"
                        tooltipPosition="top"
                      >
                        <i class="pi pi-file" aria-hidden="true"></i>
                        {{ assignment.tests?.length || 0 }}
                      </span>
                      <span
                        class="inline-flex align-items-center gap-2 text-color-secondary"
                        pTooltip="הגשות"
                        tooltipPosition="top"
                      >
                        <i class="pi pi-send" aria-hidden="true"></i>
                        {{ assignment.submissionsCount }}
                      </span>
                    </div>
                  </td>

                  <td class="text-center">
                    <div class="flex justify-content-center gap-1">
                      <p-button
                        icon="pi pi-pencil"
                        [text]="true"
                        pTooltip="עריכה"
                        tooltipPosition="top"
                        (onClick)="navigateToEdit(assignment.id)"
                      ></p-button>
                      <p-button
                        icon="pi pi-trash"
                        [text]="true"
                        severity="danger"
                        pTooltip="מחיקה"
                        tooltipPosition="top"
                        (onClick)="confirmDelete(assignment)"
                      ></p-button>
                    </div>
                  </td>
                </tr>
              </ng-template>

              <ng-template pTemplate="emptymessage">
                <tr>
                  <td
                    colspan="9"
                    class="text-center px-3 py-6 text-color-secondary"
                  >
                    אין תרגילים להצגה.
                  </td>
                </tr>
              </ng-template>
            </p-table>
          </div>

          <!-- Mobile cards -->
          <div class="mobile-only px-3 pb-3">
            <p-dataView
              [value]="filteredAssignments"
              [loading]="loading"
              layout="list"
            >
              <ng-template pTemplate="list" let-items>
                <div class="card-list">
                  <div *ngFor="let item of items" class="mobile-card">
                    <div class="mobile-card__header">
                      <div class="mobile-card__title">{{ item.title }}</div>
                      <div class="mobile-card__subtitle">
                        {{ item.description }}
                      </div>
                    </div>

                    <div class="mobile-card__meta">
                      <div>
                        <span class="label">דדליין</span>
                        {{ item.dueDate | date: "shortDate" }}
                      </div>
                      <div>
                        <span class="label">נקודות</span> {{ item.maxScore }}
                      </div>
                      <div>
                        <span class="label">בדיקות</span>
                        {{ item.tests?.length ?? 0 }}
                      </div>
                      <div>
                        <span class="label">הגשות</span>
                        {{ item.submissionsCount }}
                      </div>
                    </div>

                    <div class="mobile-card__actions">
                      <p-button
                        icon="pi pi-pencil"
                        [text]="true"
                        (onClick)="navigateToEdit(item.id)"
                      ></p-button>
                      <p-button
                        icon="pi pi-trash"
                        severity="danger"
                        [text]="true"
                        (onClick)="confirmDelete(item)"
                      ></p-button>
                    </div>
                  </div>
                </div>
              </ng-template>

              <ng-template pTemplate="empty">
                <div
                  class="flex flex-column align-items-center justify-content-center py-6 text-color-secondary"
                >
                  <i class="pi pi-inbox text-4xl mb-3" aria-hidden="true"></i>
                  <div class="font-bold text-lg mb-2 text-color">
                    אין תרגילים
                  </div>
                  <p-button
                    label="תרגיל חדש"
                    icon="pi pi-plus"
                    styleClass="sg-btn-primary"
                    (onClick)="navigateToCreate()"
                  ></p-button>
                </div>
              </ng-template>
            </p-dataView>
          </div>
        </p-card>
      </div>
    </section>

    <p-confirmDialog></p-confirmDialog>
  `,
  styles: [],
})
export class AssignmentsListComponent implements OnInit {
  private readonly assignmentsService = inject(AssignmentsService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly messageService = inject(MessageService);

  assignments: AssignmentExtended[] = [];
  loading = false;
  lessonId!: number;

  query = "";

  get filteredAssignments(): AssignmentExtended[] {
    const q = this.query.trim().toLowerCase();
    if (!q) return this.assignments;

    return this.assignments.filter(
      (a) =>
        (a.title ?? "").toLowerCase().includes(q) ||
        (a.description ?? "").toLowerCase().includes(q),
    );
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get("lessonId");
    if (id) {
      this.lessonId = Number(id);
      this.loadAssignments();
    }
  }

  loadAssignments(): void {
    this.loading = true;
    this.assignmentsService.getByLesson(this.lessonId).subscribe({
      next: (data: AssignmentExtended[]) => {
        this.assignments = data;
        this.loading = false;
      },
      error: (_error: unknown) => {
        this.messageService.add({
          severity: "error",
          summary: "Error",
          detail: "Failed to load assignments",
        });
        this.loading = false;
      },
    });
  }

  navigateToLessons(): void {
    this.router.navigate(["/lessons"]);
  }

  navigateToCreate(): void {
    this.router.navigate(["/lessons", this.lessonId, "assignments", "new"]);
  }

  navigateToEdit(assignmentId: number): void {
    this.router.navigate([
      "/lessons",
      this.lessonId,
      "assignments",
      assignmentId,
      "edit",
    ]);
  }

  confirmDelete(assignment: AssignmentResponseDto): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete "${assignment.title}"?`,
      header: "Confirm Delete",
      icon: "pi pi-exclamation-triangle",
      accept: () => this.deleteAssignment(assignment.id),
    });
  }

  deleteAssignment(assignmentId: number): void {
    this.assignmentsService.delete(this.lessonId, assignmentId).subscribe({
      next: () => {
        this.messageService.add({
          severity: "success",
          summary: "Success",
          detail: "Assignment deleted successfully",
        });
        this.loadAssignments();
      },
      error: (_error: unknown) => {
        this.messageService.add({
          severity: "error",
          summary: "Error",
          detail: "Failed to delete assignment",
        });
      },
    });
  }

  getStatusSeverity(
    status: string | undefined,
  ): "success" | "info" | "warning" | "danger" | "secondary" | "contrast" {
    if (status === "Closed") return "success";
    if (status === "Active") return "info";
    if (status === "Draft") return "warning";
    return "secondary";
  }

  getScoreColor(score: number | undefined): string {
    if (score === undefined || score === null) return "#64748b";
    if (score >= 90) return "#10b981";
    if (score >= 80) return "#3b82f6";
    if (score >= 70) return "#f59e0b";
    return "#ef4444";
  }
}
