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
import { DropdownModule } from "primeng/dropdown";
import { InputTextModule } from "primeng/inputtext";
import { KnobModule } from "primeng/knob";
import { RatingModule } from "primeng/rating";
import { SliderModule } from "primeng/slider";
import { TableModule } from "primeng/table";
import { TagModule } from "primeng/tag";

import { StudentResponseDto } from "@models/student.model";
import { StudentsService } from "@services/students.service";

type StudentRowVm = StudentResponseDto & {
  gradeAvg: number; // 0-100
  attendance: number; // 0-100
  onTimeFraction: { a: number; b: number };
  stars: number; // 0-5
};

@Component({
  selector: "app-students-list",
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    DataViewModule,
    ButtonModule,
    CardModule,
    ConfirmDialogModule,
    InputTextModule,
    DropdownModule,
    KnobModule,
    RatingModule,
    TagModule,
    SliderModule,
    ChipModule,
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

  students: StudentRowVm[] = [];
  loading = false;

  // Filters (right panel)
  query = "";
  classFilter: string | null = null;
  gradeMin = 80; // slider (0-100)
  onlyFavorites = false;
  footerRating = 4;

  pageSizeOptions = [6, 10, 20, 50].map((x) => ({
    label: String(x),
    value: x,
  }));
  rows = 6;

  get classOptions() {
    const classes = Array.from(
      new Set(this.students.map((s) => s.className).filter(Boolean)),
    ) as string[];
    return [
      { label: "כל הכיתות", value: null },
      ...classes.map((c) => ({ label: c, value: c })),
    ];
  }

  ngOnInit(): void {
    this.loadStudents();
  }

  loadStudents(): void {
    this.loading = true;

    this.studentsService.getAll().subscribe({
      next: (data) => {
        this.students = data.map((s, idx) => this.toVm(s, idx));
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

  private toVm(s: StudentResponseDto, idx: number): StudentRowVm {
    // Demo values (until backend provides real metrics)
    const gradeAvg = [0, 70, 0, 100, 0][idx % 5] ?? 0;
    const attendance = [0, 0, 0, 100, 0][idx % 5] ?? 0;
    const onTimeA = [12, 12, 0, 1, 0][idx % 5] ?? 0;
    const onTimeB = [40, 40, 30, 30, 30][idx % 5] ?? 30;
    const stars = [0, 0, 0, 0, 0][idx % 5] ?? 0;

    return {
      ...s,
      gradeAvg,
      attendance,
      onTimeFraction: { a: onTimeA, b: onTimeB },
      stars,
    };
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
      message: `בטוחה שברצונך למחוק את "${student.fullName ?? ""}"?`,
      header: "אישור מחיקה",
      icon: "pi pi-exclamation-triangle",
      acceptLabel: "מחקי",
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
    this.gradeMin = 80;
    this.onlyFavorites = false;
  }
}
