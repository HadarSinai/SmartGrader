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
import { TagModule } from "primeng/tag";
import { ToggleButtonModule } from "primeng/togglebutton";
import { TooltipModule } from "primeng/tooltip";

import { SchoolClassResponseDto } from "@models/class.model";
import { AuthService } from "@services/auth.service";
import { ClassesService } from "@services/classes.service";

@Component({
  selector: "app-classes-list",
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
    MenuModule,
    ToggleButtonModule,
    TooltipModule,
  ],
  providers: [ConfirmationService, MessageService],
  templateUrl: "./classes-list.component.html",
  styleUrls: ["./classes-list.component.css"],
})
export class ClassesListComponent implements OnInit {
  private readonly classesService = inject(ClassesService);
  private readonly router = inject(Router);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly messageService = inject(MessageService);
  readonly auth = inject(AuthService);

  classes: SchoolClassResponseDto[] = [];
  loading = false;
  query = "";
  includeArchived = false;

  rowMenuItems: MenuItem[] = [];

  get filteredClasses(): SchoolClassResponseDto[] {
    const q = this.query.trim().toLowerCase();
    return this.classes.filter(
      (c) => !q || c.name.toLowerCase().includes(q),
    );
  }

  ngOnInit(): void {
    this.loadClasses();
  }

  loadClasses(): void {
    this.loading = true;
    this.classesService.getAll(this.includeArchived).subscribe({
      next: (data) => {
        this.classes = data;
        this.loading = false;
      },
      error: () => {
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "טעינת הכיתות נכשלה",
        });
        this.loading = false;
      },
    });
  }

  openRowMenu(
    event: Event,
    menu: Menu,
    schoolClass: SchoolClassResponseDto,
  ): void {
    this.rowMenuItems = [
      {
        label: "עריכה",
        icon: "pi pi-pencil",
        command: () => this.navigateToEdit(schoolClass.id),
      },
      {
        label: "מחיקה",
        icon: "pi pi-trash",
        styleClass: "sg-menu-danger",
        command: () => this.confirmDelete(schoolClass),
      },
    ];
    menu.toggle(event);
  }

  navigateToCreate(): void {
    this.router.navigate(["/classes/new"]);
  }

  navigateToEdit(id: number): void {
    this.router.navigate(["/classes", id, "edit"]);
  }

  confirmDelete(schoolClass: SchoolClassResponseDto): void {
    if (schoolClass.studentsCount > 0) {
      this.messageService.add({
        severity: "warn",
        summary: "לא ניתן למחוק",
        detail:
          "לא ניתן למחוק כיתה שיש בה תלמידים — אפשר להעביר לארכיון בסיום שנה",
      });
      return;
    }

    this.confirmationService.confirm({
      message: `האם למחוק את הכיתה "${schoolClass.name}"? לא ניתן לשחזר פעולה זו.`,
      header: "אישור מחיקה",
      icon: "pi pi-exclamation-triangle",
      acceptLabel: "מחיקה",
      rejectLabel: "ביטול",
      accept: () => this.deleteClass(schoolClass.id),
    });
  }

  deleteClass(id: number): void {
    this.classesService.delete(id).subscribe({
      next: () => {
        this.messageService.add({
          severity: "success",
          summary: "בוצע",
          detail: "הכיתה נמחקה בהצלחה",
        });
        this.loadClasses();
      },
      error: () => {
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "מחיקת הכיתה נכשלה",
        });
      },
    });
  }

  confirmFinishYear(): void {
    const activeClasses = this.classes.filter((c) => !c.isArchived);
    const studentsCount = activeClasses.reduce(
      (sum, c) => sum + c.studentsCount,
      0,
    );

    this.confirmationService.confirm({
      message:
        `פעולה זו תעביר לארכיון ${activeClasses.length} כיתות פעילות ` +
        `(${studentsCount} תלמידים). התלמידים והציונים יישמרו ויהיו זמינים ` +
        `לצפייה בארכיון. האם להמשיך?`,
      header: "סיום שנת לימודים",
      icon: "pi pi-exclamation-triangle",
      acceptLabel: "סיום שנה",
      rejectLabel: "ביטול",
      accept: () => this.finishYear(),
    });
  }

  finishYear(): void {
    this.classesService.finishYear().subscribe({
      next: (result) => {
        this.messageService.add({
          severity: "success",
          summary: "בוצע",
          detail: `${result.archivedCount} כיתות הועברו לארכיון`,
        });
        this.loadClasses();
      },
      error: () => {
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "סיום השנה נכשל",
        });
      },
    });
  }
}
