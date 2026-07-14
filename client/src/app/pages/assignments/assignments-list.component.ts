import { CommonModule } from "@angular/common";
import { Component, OnInit, inject } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { ActivatedRoute, Router } from "@angular/router";
import { AssignmentResponseDto } from "@models/assignment.model";
import { AssignmentsService } from "@services/assignments.service";
import { ConfirmationService, MenuItem, MessageService } from "primeng/api";
import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { ChipModule } from "primeng/chip";
import { ConfirmDialogModule } from "primeng/confirmdialog";
import { DataViewModule } from "primeng/dataview";
import { InputTextModule } from "primeng/inputtext";
import { Menu, MenuModule } from "primeng/menu";
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
    TooltipModule,
    DataViewModule,
    MenuModule,
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
                <a
                  class="sg-breadcrumb-link"
                  role="link"
                  tabindex="0"
                  (click)="navigateToLessons()"
                  (keydown.enter)="navigateToLessons()"
                >
                  <i class="pi pi-arrow-right" aria-hidden="true"></i>
                  חזרה לשיעורים
                </a>
                <div class="sg-h1">תרגילים</div>
                <div class="sg-h2">ניהול תרגילים לשיעור הנבחר</div>
              </div>

              <p-button
                label="תרגיל חדש"
                icon="pi pi-plus"
                styleClass="sg-btn-primary"
                (onClick)="navigateToCreate()"
              >
              </p-button>
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

          <!-- Selection toolbar (design only — bulk delete is a future task) -->
          <div
            class="sg-selection-bar"
            *ngIf="selectedAssignments.length > 0"
            aria-live="polite"
          >
            <span>נבחרו {{ selectedAssignments.length }}</span>
            <p-button
              label="מחיקת נבחרים"
              icon="pi pi-trash"
              severity="danger"
              [text]="true"
              (onClick)="bulkDeleteComingSoon()"
            ></p-button>
            <p-button
              label="ביטול בחירה"
              [text]="true"
              (onClick)="clearSelection()"
            ></p-button>
          </div>

          <!-- Desktop table -->
          <div class="sg-table-wrap desktop-only">
            <p-table
              [value]="filteredAssignments"
              [loading]="loading"
              responsiveLayout="scroll"
              [paginator]="true"
              [rows]="10"
              [rowsPerPageOptions]="[10, 25, 50]"
              dataKey="id"
              [(selection)]="selectedAssignments"
              styleClass="sg-table"
            >
              <ng-template pTemplate="header">
                <tr>
                  <th style="width: 3rem">
                    <p-tableHeaderCheckbox
                      aria-label="בחירת כל השורות"
                    ></p-tableHeaderCheckbox>
                  </th>
                  <th pSortableColumn="title">
                    שם <p-sortIcon field="title"></p-sortIcon>
                  </th>
                  <th class="text-center">מקרי בדיקה</th>
                  <th class="text-center">הגשות</th>
                  <th class="text-center" style="width: 5rem">פעולות</th>
                </tr>
              </ng-template>

              <ng-template pTemplate="body" let-assignment>
                <tr>
                  <td>
                    <p-tableCheckbox
                      [value]="assignment"
                      [attr.aria-label]="'בחירת ' + (assignment.title || '')"
                    ></p-tableCheckbox>
                  </td>
                  <td>
                    <div class="flex flex-column gap-1">
                      <div class="flex align-items-center gap-2 flex-wrap">
                        <div class="font-bold text-color">
                          {{ assignment.title }}
                        </div>
                        <p-chip
                          *ngIf="assignment.isBonus"
                          label="בונוס +{{ assignment.bonusValue }}"
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

                  <td class="text-center text-color-secondary">
                    {{ assignment.tests?.length || 0 }}
                  </td>

                  <td class="text-center text-color-secondary">
                    {{ assignment.submissionsCount }}
                  </td>

                  <td class="text-center">
                    <p-button
                      icon="pi pi-ellipsis-h"
                      [text]="true"
                      [attr.aria-label]="
                        'פעולות נוספות: ' + (assignment.title || '')
                      "
                      (onClick)="openRowMenu($event, rowMenu, assignment)"
                    ></p-button>
                  </td>
                </tr>
              </ng-template>

              <ng-template pTemplate="emptymessage">
                <tr>
                  <td
                    colspan="5"
                    class="text-center px-3 py-6 text-color-secondary"
                  >
                    <div
                      class="flex flex-column align-items-center justify-content-center gap-3"
                    >
                      <i class="pi pi-inbox text-4xl" aria-hidden="true"></i>
                      <div>אין תרגילים להצגה.</div>
                      <p-button
                        label="תרגיל חדש"
                        icon="pi pi-plus"
                        styleClass="sg-btn-primary"
                        (onClick)="navigateToCreate()"
                      ></p-button>
                    </div>
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
                        icon="pi pi-ellipsis-h"
                        [text]="true"
                        [attr.aria-label]="
                          'פעולות נוספות: ' + (item.title || '')
                        "
                        (onClick)="openRowMenu($event, rowMenu, item)"
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

    <p-menu
      #rowMenu
      [popup]="true"
      appendTo="body"
      [model]="rowMenuItems"
      styleClass="sg-row-menu"
    ></p-menu>

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

  assignments: AssignmentResponseDto[] = [];
  loading = false;
  lessonId!: number;

  query = "";

  // Multi-select (design only — no real bulk delete)
  selectedAssignments: AssignmentResponseDto[] = [];

  rowMenuItems: MenuItem[] = [];

  get filteredAssignments(): AssignmentResponseDto[] {
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
      next: (data: AssignmentResponseDto[]) => {
        this.assignments = data;
        this.loading = false;
      },
      error: (_error: unknown) => {
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "טעינת התרגילים נכשלה",
        });
        this.loading = false;
      },
    });
  }

  navigateToLessons(): void {
    this.router.navigate(["/lessons"]);
  }

  openRowMenu(
    event: Event,
    menu: Menu,
    assignment: AssignmentResponseDto,
  ): void {
    this.rowMenuItems = [
      {
        label: "עריכה",
        icon: "pi pi-pencil",
        command: () => this.navigateToEdit(assignment.id),
      },
      {
        label: "מחיקה",
        icon: "pi pi-trash",
        styleClass: "sg-menu-danger",
        command: () => this.confirmDelete(assignment),
      },
    ];
    menu.toggle(event);
  }

  bulkDeleteComingSoon(): void {
    this.messageService.add({
      severity: "info",
      summary: "בקרוב",
      detail: "מחיקה מרובה תהיה זמינה בקרוב",
    });
  }

  clearSelection(): void {
    this.selectedAssignments = [];
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
      message: `האם למחוק את התרגיל "${assignment.title}"? לא ניתן לשחזר פעולה זו.`,
      header: "אישור מחיקה",
      icon: "pi pi-exclamation-triangle",
      acceptLabel: "מחיקה",
      rejectLabel: "ביטול",
      accept: () => this.deleteAssignment(assignment.id),
    });
  }

  deleteAssignment(assignmentId: number): void {
    this.assignmentsService.delete(this.lessonId, assignmentId).subscribe({
      next: () => {
        this.messageService.add({
          severity: "success",
          summary: "בוצע",
          detail: "התרגיל נמחק בהצלחה",
        });
        this.loadAssignments();
      },
      error: (_error: unknown) => {
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "מחיקת התרגיל נכשלה",
        });
      },
    });
  }
}
