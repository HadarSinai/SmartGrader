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
  template: `
    <section class="sg-page">
      <div class="pt-3 pb-5">
        <p-card styleClass="sg-card">
          <ng-template pTemplate="header">
            <div
              class="flex flex-column md:flex-row md:align-items-end md:justify-content-between gap-3 px-4 pt-4 pb-2"
            >
              <div class="sg-title">
                <div class="sg-h1">כיתות</div>
                <div class="sg-h2">ניהול כיתות ושנות לימודים</div>
              </div>

              <div class="flex flex-wrap align-items-center gap-2">
                <!-- ⚠️ מנהלת בלבד. "סיום שנה" מארכב את *כל* הכיתות הפעילות במוסד בבת
                     אחת, לא רק את אלה של המורה שלחצה, ואין undo. הבקרה האמיתית היא
                     [Authorize(Roles = "Admin")] על POST /api/classes/finish-year;
                     ההסתרה כאן היא כדי לא להציע פעולה שתחזור ב-403. -->
                @if (auth.isAdmin()) {
                  <p-button
                    label="סיום שנה"
                    icon="pi pi-calendar-times"
                    [outlined]="true"
                    styleClass="sg-btn-secondary"
                    (onClick)="confirmFinishYear()"
                  ></p-button>
                }

                <p-button
                  label="כיתה חדשה"
                  icon="pi pi-plus"
                  styleClass="sg-btn-primary"
                  (onClick)="navigateToCreate()"
                ></p-button>
              </div>
            </div>

            <!-- Search + archive toggle row -->
            <div
              class="flex flex-column md:flex-row md:align-items-center gap-3 px-4 pb-3"
            >
              <span class="p-input-icon-right sg-search">
                <i class="pi pi-search" aria-hidden="true"></i>
                <input
                  pInputText
                  type="text"
                  [(ngModel)]="query"
                  placeholder="חיפוש לפי שם כיתה..."
                  aria-label="חיפוש כיתות"
                />
              </span>

              <p-toggleButton
                [(ngModel)]="includeArchived"
                onLabel="מוצג כולל ארכיון"
                offLabel="הצגת ארכיון"
                onIcon="pi pi-box"
                offIcon="pi pi-box"
                (onChange)="loadClasses()"
                aria-label="הצגת כיתות בארכיון"
              ></p-toggleButton>
            </div>
          </ng-template>

          <div class="sg-table-wrap">
            <p-table
              [value]="filteredClasses"
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
                    שם הכיתה <p-sortIcon field="name"></p-sortIcon>
                  </th>
                  <th class="text-center" pSortableColumn="academicYear">
                    שנת לימודים <p-sortIcon field="academicYear"></p-sortIcon>
                  </th>
                  <th class="text-center">תלמידים</th>
                  <th class="text-center">סטטוס</th>
                  <th class="text-center" style="width: 5rem">פעולות</th>
                </tr>
              </ng-template>

              <ng-template pTemplate="body" let-schoolClass>
                <tr [class.sg-row-archived]="schoolClass.isArchived">
                  <td>
                    <div class="font-bold text-color">
                      {{ schoolClass.name }}
                    </div>
                  </td>

                  <td class="text-center">
                    {{ schoolClass.academicYearHebrew }}
                  </td>

                  <td class="text-center">
                    <span class="text-color-secondary">
                      {{ schoolClass.studentsCount }}
                    </span>
                  </td>

                  <td class="text-center">
                    <p-tag
                      [value]="schoolClass.isArchived ? 'ארכיון' : 'פעילה'"
                      [styleClass]="
                        schoolClass.isArchived
                          ? 'sg-tag-archived'
                          : 'sg-class-tag'
                      "
                    ></p-tag>
                  </td>

                  <td class="text-center">
                    <p-button
                      *ngIf="!schoolClass.isArchived"
                      icon="pi pi-ellipsis-h"
                      [text]="true"
                      [attr.aria-label]="'פעולות נוספות: ' + schoolClass.name"
                      (onClick)="openRowMenu($event, rowMenu, schoolClass)"
                    ></p-button>
                    <span
                      *ngIf="schoolClass.isArchived"
                      class="text-color-secondary"
                      pTooltip="כיתה בארכיון — לצפייה בלבד"
                      tooltipPosition="top"
                      >—</span
                    >
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
                      <div>אין כיתות להצגה.</div>
                      <p-button
                        label="כיתה חדשה"
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
  styles: [
    `
      .sg-row-archived {
        opacity: 0.6;
      }

      :host ::ng-deep .sg-tag-archived {
        background: var(--app-surface-2);
        color: var(--app-muted);
      }
    `,
  ],
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
