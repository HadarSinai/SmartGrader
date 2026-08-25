import { CommonModule } from "@angular/common";
import { Component, OnInit, inject } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { Router } from "@angular/router";

import { ConfirmationService, MenuItem, MessageService } from "primeng/api";
import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { ChipModule } from "primeng/chip";
import { ConfirmDialogModule } from "primeng/confirmdialog";
import { InputTextModule } from "primeng/inputtext";
import { Menu, MenuModule } from "primeng/menu";
import { TableModule } from "primeng/table";
import { TagModule } from "primeng/tag";
import { TooltipModule } from "primeng/tooltip";

import { TeacherResponseDto } from "@models/teacher.model";
import { TeachersService } from "@services/teachers.service";

@Component({
  selector: "app-teachers-list",
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    ButtonModule,
    CardModule,
    ConfirmDialogModule,
    InputTextModule,
    TagModule,
    ChipModule,
    MenuModule,
    TooltipModule,
  ],
  providers: [ConfirmationService, MessageService],
  templateUrl: "./teachers-list.component.html",
  styleUrls: ["./teachers-list.component.css"],
})
export class TeachersListComponent implements OnInit {
  private readonly teachersService = inject(TeachersService);
  private readonly router = inject(Router);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly messageService = inject(MessageService);

  teachers: TeacherResponseDto[] = [];
  loading = false;

  query = "";

  rowMenuItems: MenuItem[] = [];

  get filteredTeachers(): TeacherResponseDto[] {
    const q = this.query.trim().toLowerCase();
    if (!q) return this.teachers;

    return this.teachers.filter(
      (t) =>
        (t.fullName ?? "").toLowerCase().includes(q) ||
        (t.username ?? "").toLowerCase().includes(q) ||
        (t.email ?? "").toLowerCase().includes(q),
    );
  }

  get missingEmailCount(): number {
    return this.teachers.filter((t) => !t.email).length;
  }

  ngOnInit(): void {
    this.loadTeachers();
  }

  loadTeachers(): void {
    this.loading = true;

    this.teachersService.getAll().subscribe({
      next: (data) => {
        this.teachers = data;
        this.loading = false;
      },
      error: () => {
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "טעינת המורות נכשלה",
        });
        this.loading = false;
      },
    });
  }

  openRowMenu(event: Event, menu: Menu, teacher: TeacherResponseDto): void {
    this.rowMenuItems = [
      {
        label: "עריכה",
        icon: "pi pi-pencil",
        command: () => this.navigateToEdit(teacher.id),
      },
      {
        label: "מחיקה",
        icon: "pi pi-trash",
        styleClass: "sg-menu-danger",
        command: () => this.confirmDelete(teacher),
      },
    ];
    menu.toggle(event);
  }

  navigateToCreate(): void {
    this.router.navigate(["/teachers/new"]);
  }

  navigateToEdit(id: number): void {
    this.router.navigate(["/teachers", id, "edit"]);
  }

  confirmDelete(teacher: TeacherResponseDto): void {
    this.confirmationService.confirm({
      // מחיקת מורה שיש לה שיעורים או מקצועות נחסמת בשרת, עם הודעה שאומרת כמה —
      // ולכן כאן רק מציינים שמדובר בחשבון ההתחברות עצמו.
      message: `האם למחוק את חשבון ההתחברות של "${teacher.fullName ?? ""}"? לא ניתן לשחזר פעולה זו.`,
      header: "אישור מחיקה",
      icon: "pi pi-exclamation-triangle",
      acceptLabel: "מחיקה",
      rejectLabel: "ביטול",
      accept: () => this.deleteTeacher(teacher.id),
    });
  }

  deleteTeacher(id: number): void {
    this.teachersService.delete(id).subscribe({
      next: () => {
        this.messageService.add({
          severity: "success",
          summary: "בוצע",
          detail: "החשבון נמחק בהצלחה",
        });
        this.loadTeachers();
      },
      error: () => {
        // 400 עם ההסבר מהשרת ("יש לה 3 שיעורים...") כבר מוצג על ידי
        // ApiErrorInterceptor — אין טעם לדרוס אותו כאן בהודעה כללית.
      },
    });
  }
}
