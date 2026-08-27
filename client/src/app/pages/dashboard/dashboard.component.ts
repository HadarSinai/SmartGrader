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
  templateUrl: "./dashboard.component.html",
  styleUrls: ["./dashboard.component.css"],
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
            label: "הגשות שנבדקו לאחרונה",
            value: String(recent.length),
            icon: "pi-send",
          },
          {
            label: "ממוצע ציונים (הגשות שנבדקו)",
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
    // ⚠️ מפורש: "RequirementsNotMet" אינו מכיל fail/error, והתאמת התת-מחרוזות
    // הייתה מציגה דחייה כתגית מידע ניטרלית.
    if (status === "RequirementsNotMet") return "danger";

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
