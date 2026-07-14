import { CommonModule } from "@angular/common";
import { Component, OnInit, inject } from "@angular/core";
import { Router, RouterModule } from "@angular/router";
import {
  STATUS_LABELS_HE,
  SubmissionResponseDto,
} from "@models/submission.model";
import { LessonsService } from "@services/lessons.service";
import { StudentsService } from "@services/students.service";
import { SubmissionsService } from "@services/submissions.service";
import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { SkeletonModule } from "primeng/skeleton";
import { TableModule } from "primeng/table";
import { TagModule } from "primeng/tag";
import { TooltipModule } from "primeng/tooltip";
import { forkJoin } from "rxjs";

interface KPI {
  label: string;
  value: string;
  icon: string;
}

@Component({
  selector: "app-dashboard",
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    CardModule,
    TableModule,
    TagModule,
    SkeletonModule,
    ButtonModule,
    TooltipModule,
  ],
  template: `
    <section class="sg-page">
      <div class="pt-3 pb-5">
        <div class="sg-title mb-4">
          <div class="sg-h1">לוח בקרה</div>
          <div class="sg-h2">תמונת מצב מהירה על המערכת</div>
        </div>

        <div class="grid" *ngIf="!loading">
          <div *ngFor="let kpi of kpis" class="col-12 md:col-6 lg:col-3">
            <div class="sg-kpi-card p-3 border-round-2xl">
              <div class="flex align-items-center gap-3">
                <div
                  class="sg-kpi-iconWrap flex align-items-center justify-content-center"
                >
                  <i
                    [class]="'pi ' + kpi.icon"
                    class="text-2xl sg-kpi-icon"
                  ></i>
                </div>

                <div class="flex-1">
                  <div class="text-3xl font-bold text-color">
                    {{ kpi.value }}
                  </div>
                  <div class="text-color-secondary font-medium">
                    {{ kpi.label }}
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div class="grid" *ngIf="loading">
          <div *ngFor="let i of [1, 2, 3, 4]" class="col-12 md:col-6 lg:col-3">
            <p-skeleton width="100%" height="120px"></p-skeleton>
          </div>
        </div>

        <p-card styleClass="sg-card" class="mt-4">
          <ng-template pTemplate="header">
            <div
              class="flex flex-column md:flex-row md:align-items-end md:justify-content-between gap-3 px-4 pt-4 pb-2"
            >
              <div class="sg-title">
                <div class="sg-h1">הגשות אחרונות</div>
                <div class="sg-h2">חמש ההגשות האחרונות במערכת</div>
              </div>

              <p-button
                label="לכל ההגשות"
                icon="pi pi-arrow-left"
                [text]="true"
                routerLink="/students"
                pTooltip="ההגשות מרוכזות לפי סטודנט/ית"
              ></p-button>
            </div>
          </ng-template>

          <div class="sg-table-wrap">
            <p-table
              [value]="recentSubmissions"
              [loading]="loading"
              styleClass="sg-table"
              responsiveLayout="scroll"
            >
              <ng-template pTemplate="header">
                <tr>
                  <th>סטודנט</th>
                  <th>תרגיל</th>
                  <th class="text-center">נשלח</th>
                  <th class="text-center">סטטוס</th>
                  <th class="text-center">ציון</th>
                  <th class="text-center" style="width: 5rem">צפייה</th>
                </tr>
              </ng-template>

              <ng-template pTemplate="body" let-submission>
                <tr>
                  <td>{{ submission.studentName || "—" }}</td>
                  <td>{{ submission.assignmentName || "—" }}</td>
                  <td class="text-center text-color-secondary">
                    {{ submission.submittedAt | date: "dd.MM.yy HH:mm" }}
                  </td>
                  <td class="text-center">
                    <p-tag
                      [value]="getStatusLabel(submission.status)"
                      [severity]="getStatusSeverity(submission.status)"
                    >
                    </p-tag>
                  </td>
                  <td class="text-center">
                    <span
                      class="font-semibold"
                      [class.opacity-70]="submission.score === null"
                      >{{ submission.score ?? "—" }}</span
                    >
                  </td>
                  <td class="text-center">
                    <p-button
                      icon="pi pi-eye"
                      [text]="true"
                      aria-label="צפייה בהגשה"
                      (onClick)="
                        viewSubmission(submission.studentId, submission.id)
                      "
                    >
                    </p-button>
                  </td>
                </tr>
              </ng-template>

              <ng-template pTemplate="emptymessage">
                <tr>
                  <td
                    colspan="6"
                    class="text-center px-3 py-6 text-color-secondary"
                  >
                    אין עדיין הגשות להצגה.
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
      .sg-kpi-card {
        background: var(--app-surface);
        border: 1px solid var(--app-border);
        box-shadow: var(--shadow-sm);
      }

      .sg-kpi-iconWrap {
        width: 60px;
        height: 60px;
        border-radius: var(--radius-lg);
        background: var(--app-surface);
        border: 1px solid rgba(58, 48, 40, 0.12);
      }

      .sg-kpi-icon {
        color: var(--accent);
      }
    `,
  ],
})
export class DashboardComponent implements OnInit {
  private readonly lessonsService = inject(LessonsService);
  private readonly studentsService = inject(StudentsService);
  private readonly submissionsService = inject(SubmissionsService);
  private readonly router = inject(Router);

  kpis: KPI[] = [];
  recentSubmissions: SubmissionResponseDto[] = [];
  loading = false;

  ngOnInit(): void {
    this.loadDashboardData();
  }

  loadDashboardData(): void {
    this.loading = true;

    forkJoin({
      lessons: this.lessonsService.getAll(),
      students: this.studentsService.getAll(),
      recent: this.submissionsService.getRecent(50),
    }).subscribe({
      next: ({ lessons, students, recent }) => {
        const scored = recent.filter((s) => s.score !== null);
        const average =
          scored.length > 0
            ? Math.round(
                scored.reduce((sum, s) => sum + (s.score ?? 0), 0) /
                  scored.length,
              )
            : null;

        this.kpis = [
          {
            label: "סה״כ שיעורים",
            value: String(lessons.length),
            icon: "pi-book",
          },
          {
            label: "סה״כ סטודנטים",
            value: String(students.length),
            icon: "pi-users",
          },
          {
            label: "הגשות אחרונות",
            value: String(recent.length),
            icon: "pi-send",
          },
          {
            label: "ממוצע ציונים (הגשות אחרונות)",
            value: average !== null ? String(average) : "—",
            icon: "pi-chart-line",
          },
        ];

        this.recentSubmissions = recent.slice(0, 5);
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      },
    });
  }

  getStatusLabel(status: string | null): string {
    if (!status) return "לא ידוע";
    return STATUS_LABELS_HE[status] ?? status;
  }

  getStatusSeverity(
    status: string | null,
  ): "success" | "info" | "warning" | "danger" | "secondary" | "contrast" {
    if (!status) return "secondary";
    const statusLower = status.toLowerCase();
    if (statusLower.includes("pass") || statusLower.includes("success"))
      return "success";
    if (statusLower.includes("fail") || statusLower.includes("error"))
      return "danger";
    if (statusLower.includes("pending")) return "warning";
    return "info";
  }

  viewSubmission(studentId: number, submissionId: number): void {
    this.router.navigate(["/students", studentId, "submissions", submissionId]);
  }
}
