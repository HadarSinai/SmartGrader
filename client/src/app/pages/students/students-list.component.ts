import { CommonModule } from "@angular/common";
import { Component, OnInit, inject } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { Router } from "@angular/router";

import { ConfirmationService, MenuItem, MessageService } from "primeng/api";
import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { ChipModule } from "primeng/chip";
import { ConfirmDialogModule } from "primeng/confirmdialog";
import { DropdownModule } from "primeng/dropdown";
import { InputTextModule } from "primeng/inputtext";
import { Menu, MenuModule } from "primeng/menu";
import { TableModule } from "primeng/table";
import { TagModule } from "primeng/tag";
import { TooltipModule } from "primeng/tooltip";

import { StudentResponseDto } from "@models/student.model";
import { StudentsService } from "@services/students.service";

@Component({
  selector: "app-students-list",
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    ButtonModule,
    CardModule,
    ConfirmDialogModule,
    InputTextModule,
    DropdownModule,
    TagModule,
    ChipModule,
    MenuModule,
    TooltipModule,
  ],
  providers: [ConfirmationService, MessageService],
  templateUrl: "./students-list.component.html",
  styleUrls: ["./students-list.component.css"],
})
export class StudentsListComponent implements OnInit {
  private readonly studentsService = inject(StudentsService);
  private readonly router = inject(Router);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly messageService = inject(MessageService);

  students: StudentResponseDto[] = [];
  loading = false;

  // Filters
  query = "";
  classFilter: string | null = null;
  filtersOpen = false;

  // Multi-select (design only — no real bulk delete)
  selectedStudents: StudentResponseDto[] = [];

  rowMenuItems: MenuItem[] = [];

  get classOptions() {
    const classes = Array.from(
      new Set(this.students.map((s) => s.className).filter(Boolean)),
    ) as string[];
    return [
      { label: "כל הכיתות", value: null },
      ...classes.map((c) => ({ label: c, value: c })),
    ];
  }

  get filteredStudents(): StudentResponseDto[] {
    const q = this.query.trim().toLowerCase();
    return this.students.filter(
      (s) =>
        (!q || (s.fullName ?? "").toLowerCase().includes(q)) &&
        (!this.classFilter || s.className === this.classFilter),
    );
  }

  get hasActiveFilters(): boolean {
    return !!this.query.trim() || !!this.classFilter;
  }

  ngOnInit(): void {
    this.loadStudents();
  }

  loadStudents(): void {
    this.loading = true;

    this.studentsService.getAll().subscribe({
      next: (data) => {
        this.students = data;
        this.loading = false;
      },
      error: () => {
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "טעינת הסטודנטים נכשלה",
        });
        this.loading = false;
      },
    });
  }

  toggleFilters(): void {
    this.filtersOpen = !this.filtersOpen;
  }

  openRowMenu(event: Event, menu: Menu, student: StudentResponseDto): void {
    this.rowMenuItems = [
      {
        label: "עריכה",
        icon: "pi pi-pencil",
        command: () => this.navigateToEdit(student.id),
      },
      {
        label: "מחיקה",
        icon: "pi pi-trash",
        styleClass: "sg-menu-danger",
        command: () => this.confirmDelete(student),
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
    this.selectedStudents = [];
  }

  navigateToCreate(): void {
    this.router.navigate(["/students/new"]);
  }

  navigateToEdit(id: number): void {
    this.router.navigate(["/students", id, "edit"]);
  }

  viewSubmissions(studentId: number): void {
    this.router.navigate(["/students", studentId, "submissions"]);
  }

  confirmDelete(student: StudentResponseDto): void {
    this.confirmationService.confirm({
      message: `האם למחוק את "${student.fullName ?? ""}"? לא ניתן לשחזר פעולה זו.`,
      header: "אישור מחיקה",
      icon: "pi pi-exclamation-triangle",
      acceptLabel: "מחיקה",
      rejectLabel: "ביטול",
      accept: () => this.deleteStudent(student.id),
    });
  }

  deleteStudent(id: number): void {
    this.studentsService.delete(id).subscribe({
      next: () => {
        this.messageService.add({
          severity: "success",
          summary: "בוצע",
          detail: "הסטודנט/ית נמחק/ה בהצלחה",
        });
        this.loadStudents();
      },
      error: () => {
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "מחיקת הסטודנט/ית נכשלה",
        });
      },
    });
  }

  resetFilters(): void {
    this.query = "";
    this.classFilter = null;
  }
}
