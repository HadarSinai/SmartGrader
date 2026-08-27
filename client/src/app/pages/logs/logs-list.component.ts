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
  templateUrl: "./logs-list.component.html",
  styleUrls: ["./logs-list.component.css"],
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
