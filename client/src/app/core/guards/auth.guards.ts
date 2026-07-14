import { inject } from "@angular/core";
import { CanActivateFn, Router } from "@angular/router";
import { AuthService } from "../../services/auth.service";

/** Not logged in → /login. */
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isLoggedIn()) return true;
  return router.createUrlTree(["/login"]);
};

/** Teacher-only routes. A logged-in student is sent to her own area. */
export const teacherGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isLoggedIn()) return router.createUrlTree(["/login"]);
  if (auth.isTeacher()) return true;
  return router.createUrlTree(auth.homeRoute());
};

/** Student-only routes (future student area). */
export const studentGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isLoggedIn()) return router.createUrlTree(["/login"]);
  if (auth.isStudent()) return true;
  return router.createUrlTree(["/"]);
};
