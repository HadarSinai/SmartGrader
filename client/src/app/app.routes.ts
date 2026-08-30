import { Routes } from "@angular/router";
import { AppLayoutComponent } from "./components/layout/app-layout.component";
import { StudentLayoutComponent } from "./components/layout/student-layout.component";
import {
  adminGuard,
  authGuard,
  studentGuard,
  teacherGuard,
} from "./core/guards/auth.guards";
import { AssignmentFormComponent } from "./pages/assignments/assignment-form.component";
import { AssignmentsListComponent } from "./pages/assignments/assignments-list.component";
import { ClassFormComponent } from "./pages/classes/class-form.component";
import { ClassesListComponent } from "./pages/classes/classes-list.component";
import { CourseFormComponent } from "./pages/courses/course-form.component";
import { CoursesListComponent } from "./pages/courses/courses-list.component";
import { LoginComponent } from "./pages/auth/login.component";
import { DashboardComponent } from "./pages/dashboard/dashboard.component";
import { LessonResultsListComponent } from "./pages/lesson-results/lesson-results-list.component";
import { LessonFormComponent } from "./pages/lessons/lesson-form.component";
import { LessonsListComponent } from "./pages/lessons/lessons-list.component";
import { LogsListComponent } from "./pages/logs/logs-list.component";
import { MyAssignmentsListComponent } from "./pages/my/my-assignments-list.component";
import { MyFeedbackComponent } from "./pages/my/my-feedback.component";
import { MyGradesComponent } from "./pages/my/my-grades.component";
import { MyLessonsListComponent } from "./pages/my/my-lessons-list.component";
import { SubmitCodeComponent } from "./pages/my/submit-code.component";
import { StudentFormComponent } from "./pages/students/student-form.component";
import { StudentsListComponent } from "./pages/students/students-list.component";
import { TeacherFormComponent } from "./pages/teachers/teacher-form.component";
import { TeachersListComponent } from "./pages/teachers/teachers-list.component";
import { SubmissionDetailComponent } from "./pages/submissions/submission-detail.component";
import { SubmissionFormComponent } from "./pages/submissions/submission-form.component";
import { SubmissionsListComponent } from "./pages/submissions/submissions-list.component";

export const routes: Routes = [
  { path: "login", component: LoginComponent },
  // מסכי שחזור סיסמה — ציבוריים ובלי guard, כמו מסך הכניסה: מי שמגיעה אליהם היא
  // בהגדרה מי שאינה יכולה להתחבר.
  //
  // ⚠️ loadComponent ולא component, בשונה מכל שאר הנתיבים כאן. שני המסכים האלה נטענים
  // פעם בכמה חודשים לכל היותר, וייבוא רגיל שלהם דחף את חבילת ה-JS הראשית מעל תקציב
  // ה-build (2MB) — כלומר כל טעינה של המערכת הייתה משלמת עליהם.
  {
    path: "forgot-password",
    loadComponent: () =>
      import("./pages/auth/forgot-password.component").then(
        (m) => m.ForgotPasswordComponent,
      ),
  },
  {
    path: "reset-password",
    loadComponent: () =>
      import("./pages/auth/reset-password.component").then(
        (m) => m.ResetPasswordComponent,
      ),
  },
  // ⚠️ אין נתיב "register". הרשמה עצמית נסגרה — חשבון מורה נוצר רק בידי המנהלת
  // במסך /teachers, והנקודה בשרת נמחקה גם היא.
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
      {
        path: "classes",
        component: ClassesListComponent,
        canActivate: [teacherGuard],
      },
      {
        path: "classes/new",
        component: ClassFormComponent,
        canActivate: [teacherGuard],
      },
      {
        path: "classes/:id/edit",
        component: ClassFormComponent,
        canActivate: [teacherGuard],
      },
      {
        path: "courses",
        component: CoursesListComponent,
        canActivate: [teacherGuard],
      },
      {
        path: "courses/new",
        component: CourseFormComponent,
        canActivate: [teacherGuard],
      },
      {
        path: "courses/:id/edit",
        component: CourseFormComponent,
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
        path: "students/:studentId/submissions",
        component: SubmissionsListComponent,
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
      {
        path: "logs",
        component: LogsListComponent,
        canActivate: [adminGuard],
      },
      // ניהול חשבונות מורות — מנהלת בלבד, בתוך מעטפת המורה כמו /logs
      {
        path: "teachers",
        component: TeachersListComponent,
        canActivate: [adminGuard],
      },
      {
        path: "teachers/new",
        component: TeacherFormComponent,
        canActivate: [adminGuard],
      },
      {
        path: "teachers/:id/edit",
        component: TeacherFormComponent,
        canActivate: [adminGuard],
      },
      // האזור האישי — אותה קומפוננטה בדיוק כמו ב-/my/profile, בשתי המעטפות.
      //
      // teacherGuard ולא authGuard בלבד (שכבר יושב על ההורה): הוא כולל מנהלת, ושולח
      // תלמידה ל-homeRoute שלה — כלומר ל-/my/profile, המסך הנכון עבורה במעטפת הנכונה.
      //
      // ⚠️ loadComponent מאותו נימוק בדיוק כמו במסכי שחזור הסיסמה למעלה: מסך שנפתח
      // פעם בכמה חודשים, וייבוא רגיל שלו הוסיף ~12kB לחבילה הראשית שכבר חורגת מהתקציב.
      {
        path: "profile",
        loadComponent: () =>
          import("./pages/profile/profile.component").then(
            (m) => m.ProfileComponent,
          ),
        canActivate: [teacherGuard],
      },
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
      // תיקון והגשה מחדש אחרי כשל — אותו עורך בדיוק, במצב עריכה. השרת כבר מתיר את זה
      // (UpdateSubmissionHandler: CompilationFailed / JudgeUnavailable / AiFailed בלבד).
      {
        path: "submissions/:submissionId/edit",
        component: SubmitCodeComponent,
      },
      { path: "grades", component: MyGradesComponent },
      // אותה קומפוננטה כמו ב-/profile. שדות השם והמייל חבויים לתלמידה, ומה שנשאר
      // הוא החלפת הסיסמה — הפעולה היחידה שיש לה על החשבון שלה.
      {
        path: "profile",
        loadComponent: () =>
          import("./pages/profile/profile.component").then(
            (m) => m.ProfileComponent,
          ),
      },
    ],
  },
];
