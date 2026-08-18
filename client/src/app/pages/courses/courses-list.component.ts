import { CommonModule } from "@angular/common";
import { Component, OnInit, inject } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { Router } from "@angular/router";

import { ConfirmationService, MenuItem, MessageService } from "primeng/api";
import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { ConfirmDialogModule } from "primeng/confirmdialog";
import { InputTextModule } from "primeng/inputtext";
import { Menu, MenuModule } from "primeng/menu";
import { TableModule } from "primeng/table";
import { TooltipModule } from "primeng/tooltip";

import { CourseResponseDto } from "@models/course.model";
import { CoursesService } from "@services/courses.service";

@Component({
  selector: "app-courses-list",
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    ButtonModule,
    CardModule,
    ConfirmDialogModule,
    InputTextModule,
    MenuModule,
    TooltipModule,
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
                <div class="sg-h1">מקצועות</div>
                <div class="sg-h2">המקצועות שאת מלמדת — כל שיעור משויך למקצוע</div>
              </div>

              <div class="flex flex-wrap align-items-center gap-2">
                <p-button
                  label="מקצוע חדש"
                  icon="pi pi-plus"
                  styleClass="sg-btn-primary"
                  (onClick)="navigateToCreate()"
                ></p-button>
              </div>
            </div>

            <div
              class="flex flex-column md:flex-row md:align-items-center gap-3 px-4 pb-3"
            >
              <span class="p-input-icon-right sg-search">
                <i class="pi pi-search" aria-hidden="true"></i>
                <input
                  pInputText
                  type="text"
                  [(ngModel)]="query"
                  placeholder="חיפוש לפי שם מקצוע..."
                  aria-label="חיפוש מקצועות"
                />
              </span>
            </div>
          </ng-template>

          <div class="sg-table-wrap">
            <p-table
              [value]="filteredCourses"
              [loading]="loading"
              [paginator]="true"
              [rows]="10"
              [rowsPerPageOptions]="[10, 25, 50]"
              dataKey="id"
              responsiveLayout="scroll"
              styleClass="sg-table"
            >
              <ng-template pTemplate="header">
                <tr>
                  <th pSortableColumn="name">
                    שם המקצוע <p-sortIcon field="name"></p-sortIcon>
                  </th>
                  <th class="text-center" pSortableColumn="lessonsCount">
                    שיעורים <p-sortIcon field="lessonsCount"></p-sortIcon>
                  </th>
                  <th class="text-center" style="width: 5rem">פעולות</th>
                </tr>
              </ng-template>

              <ng-template pTemplate="body" let-course>
                <tr>
                  <td>
                    <div class="font-bold text-color">{{ course.name }}</div>
                  </td>

                  <td class="text-center">
                    <span class="text-color-secondary">
                      {{ course.lessonsCount }}
                    </span>
                  </td>

                  <td class="text-center">
                    <p-button
                      icon="pi pi-ellipsis-h"
                      [text]="true"
                      [attr.aria-label]="'פעולות נוספות: ' + course.name"
                      (onClick)="openRowMenu($event, rowMenu, course)"
                    ></p-button>
                  </td>
                </tr>
              </ng-template>

              <ng-template pTemplate="emptymessage">
                <tr>
                  <td
                    colspan="3"
                    class="text-center px-3 py-6 text-color-secondary"
                  >
                    <div
                      class="flex flex-column align-items-center justify-content-center gap-3"
                    >
                      <i class="pi pi-inbox text-4xl" aria-hidden="true"></i>
                      <div>עדיין אין מקצועות. כדי ליצור שיעור צריך קודם מקצוע.</div>
                      <p-button
                        label="מקצוע חדש"
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
})
export class CoursesListComponent implements OnInit {
  private readonly coursesService = inject(CoursesService);
  private readonly router = inject(Router);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly messageService = inject(MessageService);

  courses: CourseResponseDto[] = [];
  loading = false;
  query = "";

  rowMenuItems: MenuItem[] = [];

  get filteredCourses(): CourseResponseDto[] {
    const q = this.query.trim().toLowerCase();
    return this.courses.filter((c) => !q || c.name.toLowerCase().includes(q));
  }

  ngOnInit(): void {
    this.loadCourses();
  }

  loadCourses(): void {
    this.loading = true;
    this.coursesService.getAll().subscribe({
      next: (data) => {
        this.courses = data;
        this.loading = false;
      },
      error: () => {
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "טעינת המקצועות נכשלה",
        });
        this.loading = false;
      },
    });
  }

  openRowMenu(event: Event, menu: Menu, course: CourseResponseDto): void {
    this.rowMenuItems = [
      {
        label: "עריכה",
        icon: "pi pi-pencil",
        command: () => this.navigateToEdit(course.id),
      },
      {
        label: "מחיקה",
        icon: "pi pi-trash",
        styleClass: "sg-menu-danger",
        command: () => this.confirmDelete(course),
      },
    ];
    menu.toggle(event);
  }

  navigateToCreate(): void {
    this.router.navigate(["/courses/new"]);
  }

  navigateToEdit(id: number): void {
    this.router.navigate(["/courses", id, "edit"]);
  }

  confirmDelete(course: CourseResponseDto): void {
    if (course.lessonsCount > 0) {
      this.messageService.add({
        severity: "warn",
        summary: "לא ניתן למחוק",
        detail:
          "לא ניתן למחוק מקצוע שיש בו שיעורים — יש למחוק או להעביר את השיעורים קודם",
      });
      return;
    }

    this.confirmationService.confirm({
      message: `האם למחוק את המקצוע "${course.name}"? לא ניתן לשחזר פעולה זו.`,
      header: "אישור מחיקה",
      icon: "pi pi-exclamation-triangle",
      acceptLabel: "מחיקה",
      rejectLabel: "ביטול",
      accept: () => this.deleteCourse(course.id),
    });
  }

  deleteCourse(id: number): void {
    this.coursesService.delete(id).subscribe({
      next: () => {
        this.messageService.add({
          severity: "success",
          summary: "בוצע",
          detail: "המקצוע נמחק בהצלחה",
        });
        this.loadCourses();
      },
      error: () => {
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "מחיקת המקצוע נכשלה",
        });
      },
    });
  }
}
