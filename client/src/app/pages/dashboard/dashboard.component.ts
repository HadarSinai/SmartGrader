import { CommonModule } from "@angular/common";
import { Component, OnInit, inject } from "@angular/core";
import { Router } from "@angular/router";
import { SubmissionResponseDto } from "@models/submission.model";
import { AssignmentsService } from "@services/assignments.service";
import { LessonsService } from "@services/lessons.service";
import { StudentsService } from "@services/students.service";
import { SubmissionsService } from "@services/submissions.service";
import { CardModule } from "primeng/card";
import { SkeletonModule } from "primeng/skeleton";
import { TableModule } from "primeng/table";
import { TagModule } from "primeng/tag";

interface KPI {
  label: string;
  value: number;
  icon: string;
  trend?: string;
}

@Component({
  selector: "app-dashboard",
  standalone: true,
  imports: [CommonModule, CardModule, TableModule, TagModule, SkeletonModule],
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
                  <div
                    class="text-sm mt-1"
                    class="sg-kpi-trend"
                    *ngIf="kpi.trend"
                  >
                    {{ kpi.trend }}
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
                <div class="sg-h2">צפייה מהירה והמשך עבודה</div>
              </div>
            </div>
          </ng-template>

          <div class="sg-table-wrap">
            <p-table
              [value]="recentSubmissions"
              [loading]="loading"
              styleClass="sg-table"
              responsiveLayout="scroll"
              [paginator]="true"
              [rows]="10"
            >
              <ng-template pTemplate="header">
                <tr>
                  <th class="text-center">סטודנט</th>
                  <th class="text-center">תרגיל</th>
                  <th class="text-center">נשלח</th>
                  <th class="text-center">סטטוס</th>
                  <th class="text-center">ציון</th>
                  <th class="text-center">פעולות</th>
                </tr>
              </ng-template>

              <ng-template pTemplate="body" let-submission>
                <tr>
                  <td>{{ submission.studentName || "—" }}</td>
                  <td>{{ submission.assignmentName || "—" }}</td>
                  <td class="text-center">
                    {{ submission.submittedAt | date: "short" }}
                  </td>
                  <td class="text-center">
                    <p-tag
                      [value]="submission.status || 'Unknown'"
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
        box-shadow: 0 18px 42px rgba(58, 48, 40, 0.1);
      }

      .sg-kpi-iconWrap {
        width: 60px;
        height: 60px;
        border-radius: 18px;
        background: var(--app-surface);
        border: 1px solid rgba(58, 48, 40, 0.12);
      }

      .sg-kpi-icon {
        color: var(--accent);
      }

      .sg-kpi-trend {
        color: var(--app-muted);
        font-weight: 700;
      }
    `,
  ],
})
export class DashboardComponent implements OnInit {
  private readonly lessonsService = inject(LessonsService);
  private readonly studentsService = inject(StudentsService);
  private readonly assignmentsService = inject(AssignmentsService);
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

    Promise.all([
      this.lessonsService.getAll().toPromise(),
      this.studentsService.getAll().toPromise(),
      this.assignmentsService.getAll(1).toPromise(),
      this.submissionsService.getAll(1).toPromise(),
    ]).then(([lessons, students, assignments, submissions]) => {
      const totalLessons = lessons?.length || 0;
      const totalStudents = students?.length || 0;
      const totalAssignments = assignments?.length || 0;
      const totalSubmissions = submissions?.length || 0;

      this.kpis = [
        {
          label: "סה״כ שיעורים",
          value: totalLessons,
          icon: "pi-book",
          trend: "+2 השבוע",
        },
        {
          label: "סה״כ סטודנטים",
          value: totalStudents,
          icon: "pi-users",
          trend: "+5 החודש",
        },
        {
          label: "תרגילים",
          value: totalAssignments,
          icon: "pi-file-edit",
          trend: "+3 פעילים",
        },
        {
          label: "הגשות",
          value: totalSubmissions,
          icon: "pi-send",
          trend: "+12 היום",
        },
      ];

      this.loadRecentSubmissions();
    });
  }

  loadRecentSubmissions(): void {
    this.submissionsService.getRecent(10).subscribe({
      next: (data: SubmissionResponseDto[]) => {
        this.recentSubmissions = data;
        this.loading = false;
      },
      error: (_error: unknown) => {
        this.loading = false;
      },
    });
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
