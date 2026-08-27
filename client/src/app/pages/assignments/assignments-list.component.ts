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
  templateUrl: "./assignments-list.component.html",
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
