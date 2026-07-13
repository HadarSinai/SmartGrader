import { Routes } from "@angular/router";
import { AppLayoutComponent } from "./components/layout/app-layout.component";
import { AssignmentFormComponent } from "./pages/assignments/assignment-form.component";
import { AssignmentsListComponent } from "./pages/assignments/assignments-list.component";
import { DashboardComponent } from "./pages/dashboard/dashboard.component";
import { LessonResultDetailComponent } from "./pages/lesson-results/lesson-result-detail.component";
import { LessonResultsListComponent } from "./pages/lesson-results/lesson-results-list.component";
import { LessonFormComponent } from "./pages/lessons/lesson-form.component";
import { LessonsListComponent } from "./pages/lessons/lessons-list.component";
import { StudentFormComponent } from "./pages/students/student-form.component";
import { StudentsListComponent } from "./pages/students/students-list.component";
import { SubmissionDetailComponent } from "./pages/submissions/submission-detail.component";
import { SubmissionFormComponent } from "./pages/submissions/submission-form.component";
import { SubmissionsListComponent } from "./pages/submissions/submissions-list.component";

export const routes: Routes = [
  {
    path: "",
    component: AppLayoutComponent,
    children: [
      { path: "", component: DashboardComponent },
      {
        path: "lessons",
        component: LessonsListComponent,
        data: {
          heroImage: "/assets/hero-witeBord1.jpg",
          heroTitle: "שיעורים",
          heroSubtitle: "ניהול שיעורים וקורסים",
        },
      },
      // Figma-like Students page (primary UI)
      {
        path: "students",
        component: StudentsListComponent,
        data: {
          heroImage: "/assets/hero-classroom.jpg",
          heroTitle: "סטודנטים",
          heroSubtitle: "ניהול סטודנטים ומעקב אחר ביצועים",
        },
      },
      // Forms
      { path: "students/new", component: StudentFormComponent },
      { path: "students/:id/edit", component: StudentFormComponent },
      { path: "lessons/new", component: LessonFormComponent },
      { path: "lessons/:id/edit", component: LessonFormComponent },
      {
        path: "lessons/:lessonId/assignments",
        component: AssignmentsListComponent,
      },
      {
        path: "lessons/:lessonId/assignments/new",
        component: AssignmentFormComponent,
      },
      {
        path: "lessons/:lessonId/assignments/:assignmentId/edit",
        component: AssignmentFormComponent,
      },
      {
        path: "lessons/:lessonId/results",
        component: LessonResultsListComponent,
      },
      {
        path: "students/:studentId/lessons/:lessonId/result",
        component: LessonResultDetailComponent,
      },
      {
        path: "students/:studentId/submissions",
        component: SubmissionsListComponent,
      },
      {
        path: "students/:studentId/submissions/new",
        component: SubmissionFormComponent,
      },
      {
        path: "students/:studentId/submissions/:submissionId",
        component: SubmissionDetailComponent,
      },
      {
        path: "students/:studentId/submissions/:submissionId/edit",
        component: SubmissionFormComponent,
      },
      { path: "assignments", redirectTo: "lessons", pathMatch: "full" },
      { path: "submissions", redirectTo: "students", pathMatch: "full" },
    ],
  },
];
