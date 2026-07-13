import { CommonModule } from "@angular/common";
import { Component, OnInit, inject } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { Router } from "@angular/router";

import { ConfirmationService, MessageService } from "primeng/api";
import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { ChipModule } from "primeng/chip";
import { ConfirmDialogModule } from "primeng/confirmdialog";
import { DataViewModule } from "primeng/dataview";
import { InputTextModule } from "primeng/inputtext";
import { TableModule } from "primeng/table";
import { TooltipModule } from "primeng/tooltip";

import { LessonResponseDto } from "@models/lesson.model";
import { LessonsService } from "@services/lessons.service";

@Component({
  selector: "app-lessons-list",
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    InputTextModule,
    ChipModule,
    TableModule,
    DataViewModule,
    ButtonModule,
    CardModule,
    ConfirmDialogModule,
    TooltipModule,
  ],
  providers: [ConfirmationService],
  styleUrls: ["./lessons-list.component.css"],
  template: `
    <section class="sg-page">
      <div class="grid align-items-start pt-3 pb-5">
        <div class="col-12 lg:col-9 order-1 lg:order-2">
          <p-card styleClass="sg-card">
            <ng-template pTemplate="header">
              <div
                class="flex flex-column md:flex-row md:align-items-end md:justify-content-between gap-3 px-4 pt-4 pb-2"
              >
                <div class="sg-title">
                  <div class="sg-h1">שיעורים</div>
                  <div class="sg-h2">ניהול שיעורים ותוכן לימודי</div>
                </div>

                <span class="p-input-icon-right sg-search">
                  <i class="pi pi-search" aria-hidden="true"></i>
                  <input
                    pInputText
                    type="text"
                    [(ngModel)]="query"
                    placeholder="חיפוש שיעורים..."
                    aria-label="חיפוש שיעורים"
                  />
                </span>
              </div>

              <div
                class="sg-active-filters px-4 pb-3"
                aria-label="מסננים פעילים"
              >
                <p-chip
                  *ngIf="query.trim()"
                  label="שם/נושא: {{ query.trim() }}"
                  [removable]="true"
                  (onRemove)="query = ''"
                >
                </p-chip>
              </div>
            </ng-template>

            <!-- Desktop table -->
            <div class="sg-table-wrap desktop-only">
              <p-table
                [value]="filteredLessons"
                [loading]="loading"
                responsiveLayout="scroll"
                styleClass="sg-table"
              >
                <ng-template pTemplate="header">
                  <tr>
                    <th class="text-center">שם</th>
                    <th class="text-center">נושא</th>
                    <th class="text-center">תאריך</th>
                    <th class="text-center">מורה</th>
                    <th class="text-center">תרגילים</th>
                    <th class="text-center">פעולות</th>
                  </tr>
                </ng-template>

                <ng-template pTemplate="body" let-lesson>
                  <tr>
                    <td>{{ lesson.name || "—" }}</td>
                    <td>{{ lesson.subject || "—" }}</td>
                    <td class="text-center">
                      {{ lesson.lessonDate | date: "short" }}
                    </td>
                    <td>{{ lesson.teacherName || "—" }}</td>
                    <td class="text-center">
                      <p-button
                        [label]="(lesson.assignmentsCount ?? 0).toString()"
                        icon="pi pi-file"
                        [text]="true"
                        [outlined]="true"
                        (onClick)="viewAssignments(lesson.id)"
                      >
                      </p-button>
                    </td>
                    <td class="text-center">
                      <div class="flex justify-content-center gap-1">
                        <p-button
                          icon="pi pi-chart-bar"
                          [text]="true"
                          pTooltip="תוצאות"
                          tooltipPosition="top"
                          [attr.aria-label]="
                            'תוצאות שיעור: ' + (lesson.name || '')
                          "
                          (onClick)="viewResults(lesson.id)"
                        ></p-button>
                        <p-button
                          icon="pi pi-pencil"
                          [text]="true"
                          [attr.aria-label]="
                            'עריכת שיעור: ' + (lesson.name || '')
                          "
                          (onClick)="navigateToEdit(lesson.id)"
                        ></p-button>
                        <p-button
                          icon="pi pi-trash"
                          [text]="true"
                          severity="danger"
                          [attr.aria-label]="
                            'מחיקת שיעור: ' + (lesson.name || '')
                          "
                          (onClick)="confirmDelete(lesson)"
                        ></p-button>
                      </div>
                    </td>
                  </tr>
                </ng-template>

                <ng-template pTemplate="emptymessage">
                  <tr>
                    <td
                      colspan="6"
                      class="text-center px-3 py-6 text-color-secondary"
                    >
                      אין שיעורים להצגה.
                    </td>
                  </tr>
                </ng-template>
              </p-table>
            </div>

            <!-- Mobile cards -->
            <div class="mobile-only px-3 pb-3">
              <p-dataView
                [value]="filteredLessons"
                [loading]="loading"
                layout="list"
              >
                <ng-template pTemplate="list" let-items>
                  <div class="card-list">
                    <div *ngFor="let item of items" class="mobile-card">
                      <div class="mobile-card__header">
                        <div class="mobile-card__title">
                          {{ item.name || "—" }}
                        </div>
                        <div class="mobile-card__subtitle">
                          {{ item.subject || "—" }}
                        </div>
                      </div>

                      <div class="mobile-card__meta">
                        <div>
                          <span class="label">תאריך</span>
                          {{ item.lessonDate | date: "shortDate" }}
                        </div>
                        <div>
                          <span class="label">מורה</span>
                          {{ item.teacherName || "—" }}
                        </div>
                        <div>
                          <span class="label">תרגילים</span>
                          {{ item.assignmentsCount ?? 0 }}
                        </div>
                      </div>

                      <div class="mobile-card__actions">
                        <p-button
                          label="תרגילים"
                          icon="pi pi-file"
                          [outlined]="true"
                          (onClick)="viewAssignments(item.id)"
                        ></p-button>
                        <p-button
                          label="תוצאות"
                          icon="pi pi-chart-bar"
                          [outlined]="true"
                          (onClick)="viewResults(item.id)"
                        ></p-button>
                        <p-button
                          icon="pi pi-pencil"
                          [text]="true"
                          [attr.aria-label]="
                            'עריכת שיעור: ' + (item.name || '')
                          "
                          (onClick)="navigateToEdit(item.id)"
                        ></p-button>
                        <p-button
                          icon="pi pi-trash"
                          severity="danger"
                          [text]="true"
                          [attr.aria-label]="
                            'מחיקת שיעור: ' + (item.name || '')
                          "
                          (onClick)="confirmDelete(item)"
                        ></p-button>
                      </div>
                    </div>
                  </div>
                </ng-template>
              </p-dataView>
            </div>

            <ng-template pTemplate="footer">
              <div class="sg-footer-actions">
                <div class="flex align-items-center gap-2 flex-wrap">
                  <p-button
                    label="שיעור חדש"
                    icon="pi pi-plus"
                    styleClass="sg-btn-primary"
                    (onClick)="navigateToCreate()"
                  ></p-button>
                </div>
              </div>
            </ng-template>
          </p-card>
        </div>

        <aside
          class="col-12 lg:col-3 order-2 lg:order-1"
          aria-label="סינון ומיון"
        >
          <p-card styleClass="sg-filters-card">
            <ng-template pTemplate="header">
              <div class="sg-filters-title">
                <span>סינון</span>
                <i class="pi pi-sliders-h" aria-hidden="true"></i>
              </div>
            </ng-template>

            <div class="sg-filter-block">
              <label class="sg-label" for="lessonQuery">חיפוש</label>
              <input
                id="lessonQuery"
                pInputText
                type="text"
                [(ngModel)]="query"
                placeholder="שם / נושא / מורה"
                aria-label="חיפוש שיעורים"
              />
              <div class="sg-hint">לדוגמה: מתמטיקה</div>
            </div>

            <div class="sg-filter-block">
              <p-button
                label="איפוס"
                [text]="true"
                (onClick)="query = ''"
                aria-label="איפוס סינון"
              ></p-button>
            </div>
          </p-card>
        </aside>
      </div>
    </section>

    <p-confirmDialog></p-confirmDialog>
  `,
})
export class LessonsListComponent implements OnInit {
  private readonly lessonsService = inject(LessonsService);
  private readonly router = inject(Router);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly messageService = inject(MessageService);

  lessons: LessonResponseDto[] = [];
  loading = false;

  query = "";

  get filteredLessons(): LessonResponseDto[] {
    const q = this.query.trim().toLowerCase();
    if (!q) return this.lessons;

    return this.lessons.filter(
      (l) =>
        (l.name ?? "").toLowerCase().includes(q) ||
        (l.subject ?? "").toLowerCase().includes(q) ||
        (l.teacherName ?? "").toLowerCase().includes(q),
    );
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
      error: () => {
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "טעינת השיעורים נכשלה",
        });
        this.loading = false;
      },
    });
  }

  navigateToCreate(): void {
    this.router.navigate(["/lessons/new"]);
  }

  navigateToEdit(id: number): void {
    this.router.navigate(["/lessons", id, "edit"]);
  }

  viewAssignments(lessonId: number): void {
    this.router.navigate(["/lessons", lessonId, "assignments"]);
  }

  viewResults(lessonId: number): void {
    this.router.navigate(["/lessons", lessonId, "results"]);
  }

  confirmDelete(lesson: LessonResponseDto): void {
    this.confirmationService.confirm({
      message: `האם למחוק את "${lesson.name}"? לא ניתן לשחזר פעולה זו.`,
      header: "אישור מחיקה",
      icon: "pi pi-exclamation-triangle",
      acceptLabel: "מחיקה",
      rejectLabel: "ביטול",
      accept: () => this.deleteLesson(lesson.id),
    });
  }

  deleteLesson(id: number): void {
    this.lessonsService.delete(id).subscribe({
      next: () => {
        this.messageService.add({
          severity: "success",
          summary: "בוצע",
          detail: "השיעור נמחק בהצלחה",
        });
        this.loadLessons();
      },
      error: () => {
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "מחיקת השיעור נכשלה",
        });
      },
    });
  }
}
