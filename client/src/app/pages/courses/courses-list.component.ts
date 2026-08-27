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
  templateUrl: "./courses-list.component.html",
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
