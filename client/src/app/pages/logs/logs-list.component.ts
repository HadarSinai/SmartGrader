import { CommonModule } from "@angular/common";
import { Component, OnInit, inject } from "@angular/core";
import { FormsModule } from "@angular/forms";

import { ConfirmationService, MessageService } from "primeng/api";
import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { ConfirmDialogModule } from "primeng/confirmdialog";
import { DropdownModule } from "primeng/dropdown";
import { InputTextModule } from "primeng/inputtext";
import { TableModule } from "primeng/table";
import { TagModule } from "primeng/tag";
import { TooltipModule } from "primeng/tooltip";

import { LogResponseDto } from "@models/log.model";
import { LogsService } from "@services/logs.service";

const ACTION_TYPE_LABELS: Record<string, string> = {
  AiGradingStarted: "התחלת בדיקה",
  AiGradingCompleted: "בדיקה הושלמה",
  CompilationFailed: "שגיאת קומפילציה",
  AiFailed: "כשל AI",
  JudgeUnavailable: "תקלה במערכת הבדיקה",
  UnhandledError: "שגיאה לא צפויה",
};

const SOURCE_LABELS: Record<string, string> = {
  AiWorker: "בודק אוטומטי",
  Api: "שרת",
};

@Component({
  selector: "app-logs-list",
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    CardModule,
    ConfirmDialogModule,
    DropdownModule,
    InputTextModule,
    TableModule,
    TagModule,
    TooltipModule,
  ],
  providers: [ConfirmationService, MessageService],
  template: `
    <p-confirmDialog [rtl]="true"></p-confirmDialog>

    <section class="sg-page">
      <div class="pt-3 pb-5">
        <p-card styleClass="sg-card">
          <ng-template pTemplate="header">
            <div
              class="flex flex-column md:flex-row md:align-items-end md:justify-content-between gap-3 px-4 pt-4 pb-2"
            >
              <div class="sg-title">
                <div class="sg-h1">יומן מערכת</div>
                <div class="sg-h2">
                  אירועי בדיקה אוטומטית ושגיאות מערכת ({{ logs.length }}
                  רשומות אחרונות)
                </div>
              </div>

              <div class="flex align-items-center gap-2">
                <p-button
                  label="רענון"
                  icon="pi pi-refresh"
                  [text]="true"
                  [loading]="loading"
                  (onClick)="loadLogs()"
                ></p-button>
                <p-button
                  label="מחיקת לוגים ישנים"
                  icon="pi pi-trash"
                  [outlined]="true"
                  severity="danger"
                  [loading]="deleting"
                  (onClick)="confirmDeleteOld()"
                ></p-button>
              </div>
            </div>

            <!-- Filters row -->
            <div
              class="flex flex-column md:flex-row md:align-items-center gap-3 px-4 pb-3"
            >
              <span class="p-input-icon-right sg-search">
                <i class="pi pi-search" aria-hidden="true"></i>
                <input
                  pInputText
                  type="text"
                  [(ngModel)]="query"
                  placeholder="חיפוש בהודעות..."
                  aria-label="חיפוש בלוגים"
                />
              </span>

              <p-dropdown
                inputId="actionTypeFilter"
                [options]="actionTypeOptions"
                [(ngModel)]="actionTypeFilter"
                optionLabel="label"
                optionValue="value"
                placeholder="כל סוגי הפעולות"
                aria-label="סינון לפי סוג פעולה"
              ></p-dropdown>

              <p-dropdown
                inputId="statusFilter"
                [options]="statusOptions"
                [(ngModel)]="statusFilter"
                optionLabel="label"
                optionValue="value"
                placeholder="כל הסטטוסים"
                aria-label="סינון לפי סטטוס"
              ></p-dropdown>

              <p-button
                *ngIf="hasActiveFilters"
                label="איפוס"
                [text]="true"
                (onClick)="clearFilters()"
                aria-label="איפוס סינון"
              ></p-button>
            </div>
          </ng-template>

          <div class="sg-table-wrap">
            <p-table
              [value]="filteredLogs"
              [loading]="loading"
              [paginator]="true"
              [rows]="25"
              [rowsPerPageOptions]="[25, 50, 100]"
              responsiveLayout="scroll"
              styleClass="sg-table"
            >
              <ng-template pTemplate="header">
                <tr>
                  <th class="text-center" pSortableColumn="timestamp">
                    תאריך ושעה <p-sortIcon field="timestamp"></p-sortIcon>
                  </th>
                  <th class="text-center">סוג פעולה</th>
                  <th class="text-center">סטטוס</th>
                  <th class="text-center">מקור</th>
                  <th class="text-right">הודעה</th>
                </tr>
              </ng-template>

              <ng-template pTemplate="body" let-log>
                <tr>
                  <td class="text-center white-space-nowrap">
                    {{ asUtc(log.timestamp) | date: "dd.MM.yyyy HH:mm" }}
                  </td>
                  <td class="text-center">
                    <p-tag
                      [severity]="log.status === 'Error' ? 'danger' : 'info'"
                      [value]="actionTypeLabel(log.actionType)"
                    ></p-tag>
                  </td>
                  <td class="text-center">
                    <p-tag
                      *ngIf="log.status === 'Success'"
                      severity="success"
                      value="תקין"
                      icon="pi pi-check-circle"
                    ></p-tag>
                    <p-tag
                      *ngIf="log.status === 'Error'"
                      severity="danger"
                      value="שגיאה"
                      icon="pi pi-times-circle"
                    ></p-tag>
                  </td>
                  <td class="text-center">
                    {{ sourceLabel(log.systemSource) }}
                  </td>
                  <td class="text-right sg-log-message">{{ log.message }}</td>
                </tr>
              </ng-template>

              <ng-template pTemplate="emptymessage">
                <tr>
                  <td
                    colspan="5"
                    class="text-center px-3 py-6 text-color-secondary"
                  >
                    <ng-container *ngIf="hasActiveFilters; else noLogs">
                      לא נמצאו רשומות התואמות לסינון.
                      <p-button
                        label="איפוס סינון"
                        [text]="true"
                        (onClick)="clearFilters()"
                      ></p-button>
                    </ng-container>
                    <ng-template #noLogs>
                      <div class="flex flex-column align-items-center gap-2">
                        <i
                          class="pi pi-history text-3xl"
                          style="color: var(--app-muted)"
                          aria-hidden="true"
                        ></i>
                        <span>אין רשומות ביומן המערכת.</span>
                      </div>
                    </ng-template>
                  </td>
                </tr>
              </ng-template>
            </p-table>
          </div>
        </p-card>
      </div>
    </section>
  `,
  styles: [
    `
      .sg-log-message {
        max-width: 32rem;
        word-break: break-word;
      }
    `,
  ],
})
export class LogsListComponent implements OnInit {
  private readonly logsService = inject(LogsService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly messageService = inject(MessageService);

  logs: LogResponseDto[] = [];
  loading = false;
  deleting = false;

  // Filters
  query = "";
  actionTypeFilter: string | null = null;
  statusFilter: string | null = null;

  readonly statusOptions = [
    { label: "כל הסטטוסים", value: null },
    { label: "תקין", value: "Success" },
    { label: "שגיאה", value: "Error" },
  ];

  get actionTypeOptions() {
    const types = Array.from(new Set(this.logs.map((l) => l.actionType)));
    return [
      { label: "כל סוגי הפעולות", value: null },
      ...types.map((t) => ({ label: this.actionTypeLabel(t), value: t })),
    ];
  }

  get filteredLogs(): LogResponseDto[] {
    const q = this.query.trim().toLowerCase();
    return this.logs.filter(
      (l) =>
        (!q || l.message.toLowerCase().includes(q)) &&
        (!this.actionTypeFilter || l.actionType === this.actionTypeFilter) &&
        (!this.statusFilter || l.status === this.statusFilter),
    );
  }

  get hasActiveFilters(): boolean {
    return (
      !!this.query.trim() || !!this.actionTypeFilter || !!this.statusFilter
    );
  }

  ngOnInit(): void {
    this.loadLogs();
  }

  loadLogs(): void {
    this.loading = true;
    this.logsService.getAll().subscribe({
      next: (data) => {
        this.logs = data;
        this.loading = false;
      },
      error: () => {
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "טעינת יומן המערכת נכשלה",
        });
        this.loading = false;
      },
    });
  }

  actionTypeLabel(actionType: string): string {
    return ACTION_TYPE_LABELS[actionType] ?? actionType;
  }

  /** Server timestamps are UTC but may be serialized without a Z suffix. */
  asUtc(timestamp: string): string {
    return /Z|[+-]\d{2}:\d{2}$/.test(timestamp) ? timestamp : timestamp + "Z";
  }

  sourceLabel(source: string): string {
    return SOURCE_LABELS[source] ?? source;
  }

  confirmDeleteOld(): void {
    this.confirmationService.confirm({
      message:
        "האם למחוק את כל הלוגים הישנים מ-30 יום? לא ניתן לשחזר פעולה זו.",
      header: "אישור מחיקה",
      icon: "pi pi-exclamation-triangle",
      acceptLabel: "מחיקה",
      rejectLabel: "ביטול",
      accept: () => this.deleteOld(),
    });
  }

  deleteOld(): void {
    this.deleting = true;
    this.logsService.deleteOld(30).subscribe({
      next: (result) => {
        this.deleting = false;
        this.messageService.add({
          severity: "success",
          summary: "בוצע",
          detail: `נמחקו ${result.deleted} רשומות ישנות`,
        });
        this.loadLogs();
      },
      error: () => {
        this.deleting = false;
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "מחיקת הלוגים נכשלה",
        });
      },
    });
  }

  clearFilters(): void {
    this.query = "";
    this.actionTypeFilter = null;
    this.statusFilter = null;
  }
}
