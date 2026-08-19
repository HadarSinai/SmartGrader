import { inject } from "@angular/core";
import { CanActivateFn, Router } from "@angular/router";
import { MessageService } from "primeng/api";
import { AuthService } from "../../services/auth.service";

/** Not logged in → /login. */
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isLoggedIn()) return true;
  return router.createUrlTree(["/login"]);
};

/** Teacher-only routes (admin included). A logged-in student is sent to her own area. */
export const teacherGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isLoggedIn()) return router.createUrlTree(["/login"]);
  if (auth.isTeacher() || auth.isAdmin()) return true;
  return router.createUrlTree(auth.homeRoute());
};

/** Admin-only routes (e.g. the system log). */
export const adminGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isLoggedIn()) return router.createUrlTree(["/login"]);
  if (auth.isAdmin()) return true;
  return router.createUrlTree(auth.homeRoute());
};

/**
 * Student-only routes ("המסע שלי").
 *
 * ⚠️ Requires the studentId claim, not just the role. A Student login with no linked student
 * record has no studentId, and every page in the area bails before setting `loading` — blank
 * screens and a submit button that silently does nothing. Sending her to "/" instead is a
 * redirect loop: "/" is teacherGuard-protected and bounces back to homeRoute(). So: sign out
 * with a message, once, instead of dropping her into a broken area.
 */
export const studentGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const toast = inject(MessageService);

  if (!auth.isLoggedIn()) return router.createUrlTree(["/login"]);

  if (auth.isStudent()) {
    if (auth.studentId() !== null) return true;

    auth.logout();
    toast.add({
      severity: "error",
      summary: "החשבון אינו מקושר לתלמידה",
      detail:
        "החשבון שלך אינו מקושר לרשומת תלמידה במערכת, ולכן אין לו לאן להיכנס. יש לפנות למורה כדי לקשר אותו.",
      life: 8000,
    });
    return router.createUrlTree(["/login"]);
  }

  return router.createUrlTree(auth.homeRoute());
};
