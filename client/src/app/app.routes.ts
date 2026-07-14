import { Routes } from "@angular/router";
import { AppLayoutComponent } from "./components/layout/app-layout.component";
import { StudentLayoutComponent } from "./components/layout/student-layout.component";
import {
    authGuard,
    studentGuard,
    teacherGuard,
} from "./core/guards/auth.guards";
import { AssignmentFormComponent } from "./pages/assignments/assignment-form.component";
import { AssignmentsListComponent } from "./pages/assignments/assignments-list.component";
import { LoginComponent } from "./pages/auth/login.component";
import { RegisterComponent } from "./pages/auth/register.component";
import { DashboardComponent } from "./pages/dashboard/dashboard.component";
import { LessonResultDetailComponent } from "./pages/lesson-results/lesson-result-detail.component";
import { LessonResultsListComponent } from "./pages/lesson-results/lesson-results-list.component";
import { LessonFormComponent } from "./pages/lessons/lesson-form.component";
import { LessonsListComponent } from "./pages/lessons/lessons-list.component";
import { MyAssignmentsListComponent } from "./pages/my/my-assignments-list.component";
import { MyFeedbackComponent } from "./pages/my/my-feedback.component";
import { MyGradesComponent } from "./pages/my/my-grades.component";
import { MyLessonsListComponent } from "./pages/my/my-lessons-list.component";
import { SubmitCodeComponent } from "./pages/my/submit-code.component";
import { StudentFormComponent } from "./pages/students/student-form.component";
import { StudentsListComponent } from "./pages/students/students-list.component";
import { SubmissionDetailComponent } from "./pages/submissions/submission-detail.component";
import { SubmissionFormComponent } from "./pages/submissions/submission-form.component";
import { SubmissionsListComponent } from "./pages/submissions/submissions-list.component";

export const routes: Routes = [
  { path: "login", component: LoginComponent },
  { path: "register", component: RegisterComponent },
  {
    path: "",
    component: AppLayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: "", component: DashboardComponent, canActivate: [teacherGuard] },
      {
        path: "lessons",
        component: LessonsListComponent,
        canActivate: [teacherGuard],
      },
      {
        path: "students",
        component: StudentsListComponent,
        canActivate: [teacherGuard],
      },
      // Forms
      {
        path: "students/new",
        component: StudentFormComponent,
        canActivate: [teacherGuard],
      },
      {
        path: "students/:id/edit",
        component: StudentFormComponent,
        canActivate: [teacherGuard],
      },
      {
        path: "lessons/new",
        component: LessonFormComponent,
        canActivate: [teacherGuard],
      },
      {
        path: "lessons/:id/edit",
        component: LessonFormComponent,
        canActivate: [teacherGuard],
      },
      {
        path: "lessons/:lessonId/assignments",
        component: AssignmentsListComponent,
        canActivate: [teacherGuard],
      },
      {
        path: "lessons/:lessonId/assignments/new",
        component: AssignmentFormComponent,
        canActivate: [teacherGuard],
      },
      {
        path: "lessons/:lessonId/assignments/:assignmentId/edit",
        component: AssignmentFormComponent,
        canActivate: [teacherGuard],
      },
      {
        path: "lessons/:lessonId/results",
        component: LessonResultsListComponent,
        canActivate: [teacherGuard],
      },
      {
        path: "students/:studentId/lessons/:lessonId/result",
        component: LessonResultDetailComponent,
        canActivate: [teacherGuard],
      },
      {
        path: "students/:studentId/submissions",
        component: SubmissionsListComponent,
        canActivate: [teacherGuard],
      },
      {
        path: "students/:studentId/submissions/new",
        component: SubmissionFormComponent,
        canActivate: [teacherGuard],
      },
      {
        path: "students/:studentId/submissions/:submissionId",
        component: SubmissionDetailComponent,
        canActivate: [teacherGuard],
      },
      {
        path: "students/:studentId/submissions/:submissionId/edit",
        component: SubmissionFormComponent,
        canActivate: [teacherGuard],
      },
      { path: "assignments", redirectTo: "lessons", pathMatch: "full" },
      { path: "submissions", redirectTo: "students", pathMatch: "full" },
    ],
  },
  {
    path: "my",
    component: StudentLayoutComponent,
    canActivate: [studentGuard],
    children: [
      { path: "", redirectTo: "lessons", pathMatch: "full" },
      { path: "lessons", component: MyLessonsListComponent },
      {
        path: "lessons/:lessonId/assignments",
        component: MyAssignmentsListComponent,
      },
      {
        path: "lessons/:lessonId/assignments/:assignmentId/submit",
        component: SubmitCodeComponent,
      },
      { path: "submissions/:submissionId", component: MyFeedbackComponent },
      { path: "grades", component: MyGradesComponent },
    ],
  },
];
