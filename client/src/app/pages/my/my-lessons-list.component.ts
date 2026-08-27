import { CommonModule } from "@angular/common";
import { Component, OnInit } from "@angular/core";
import { Router } from "@angular/router";
import { forkJoin, of } from "rxjs";
import { catchError, map } from "rxjs/operators";

import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { TableModule } from "primeng/table";
import { TagModule } from "primeng/tag";
import { TooltipModule } from "primeng/tooltip";

import { LessonResultResponseDto } from "@models/lesson-result.model";
import { LessonResponseDto } from "@models/lesson.model";
import { AuthService } from "@services/auth.service";
import { LessonResultsService } from "@services/lesson-results.service";
import { LessonsService } from "@services/lessons.service";

interface MyLessonRow {
  lesson: LessonResponseDto;
  result: LessonResultResponseDto | null;
}

@Component({
  selector: "app-my-lessons-list",
  standalone: true,
  imports: [
    CommonModule,
    TableModule,
    ButtonModule,
    CardModule,
    TagModule,
    TooltipModule,
  ],
  templateUrl: "./my-lessons-list.component.html",
})
export class MyLessonsListComponent implements OnInit {
  rows: MyLessonRow[] = [];
  loading = false;

  constructor(
    private lessonsService: LessonsService,
    private lessonResultsService: LessonResultsService,
    private auth: AuthService,
    private router: Router,
  ) {}

  ngOnInit(): void {
    this.loadLessons();
  }

  openLesson(lessonId: number): void {
    this.router.navigate(["/my", "lessons", lessonId, "assignments"]);
  }

  private loadLessons(): void {
    const studentId = this.auth.studentId();
    if (studentId === null) return;

    this.loading = true;
    this.lessonsService.getAll().subscribe({
      next: (lessons: LessonResponseDto[]) => {
        if (lessons.length === 0) {
          this.rows = [];
          this.loading = false;
          return;
        }
        forkJoin(
          lessons.map((lesson) =>
            this.lessonResultsService.getResult(studentId, lesson.id).pipe(
              map(
                (result: LessonResultResponseDto): MyLessonRow => ({
                  lesson,
                  result,
                }),
              ),
              // 404 (no result yet) → "בתהליך"
              catchError(() => of<MyLessonRow>({ lesson, result: null })),
            ),
          ),
        ).subscribe({
          next: (rows: MyLessonRow[]) => {
            this.rows = rows;
            this.loading = false;
          },
          error: () => {
            this.loading = false;
          },
        });
      },
      error: () => {
        this.loading = false;
      },
    });
  }
}
