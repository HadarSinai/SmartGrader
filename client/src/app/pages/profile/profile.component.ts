import { CommonModule } from "@angular/common";
import { Component, OnInit, inject } from "@angular/core";
import {
  AbstractControl,
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from "@angular/forms";
import { MyProfileResponseDto } from "@models/auth.model";
import { MessageService } from "primeng/api";
import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { InputTextModule } from "primeng/inputtext";
import { PasswordModule } from "primeng/password";
import { SkeletonModule } from "primeng/skeleton";
import { PasswordChecklistComponent } from "../../components/password-checklist/password-checklist.component";
import { NoHebrewDirective } from "../../core/directives/no-hebrew.directive";
import {
  passwordStrengthValidator,
  passwordsMatch,
} from "../../core/validators/password.validator";
import { AuthService } from "../../services/auth.service";

/**
 * האזור האישי — משתמשת מתחזקת את החשבון של עצמה.
 *
 * קומפוננטה אחת על שני מסלולים, ולא שתיים: `/profile` במעטפת המורה ו-`/my/profile`
 * במעטפת התלמידה. ההבדל היחיד ביניהם הוא ששדות השם והמייל חבויים לתלמידה, ומסך שני
 * שכל תוכנו הוא בלוק החלפת הסיסמה היה עותק שיתיישן.
 *
 * ⚠️ ההסתרה כאן היא **תצוגה בלבד**. הבקרה האמיתית היא
 * `[Authorize(Roles = "Teacher,Admin")]` על `PUT /api/auth/me` בשרת: תלמידה שתקרא
 * לנקודה ישירות תקבל 403 גם אם המסך הזה יציג לה שדות בטעות.
 *
 * למה תלמידה אינה משנה את שמה: `User.FullName` ו-`Student.FullName` הם שני שדות
 * נפרדים, ושינוי צד אחד היה מפצל בין מה שהיא רואה למה שהמורה שלה רואה. מייל אין לה כלל.
 */
@Component({
  selector: "app-profile",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    CardModule,
    InputTextModule,
    ButtonModule,
    PasswordModule,
    SkeletonModule,
    PasswordChecklistComponent,
    NoHebrewDirective,
  ],
  templateUrl: "./profile.component.html",
  styleUrls: ["./profile.component.css"],
})
export class ProfileComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly messageService = inject(MessageService);

  readonly auth = inject(AuthService);

  profile: MyProfileResponseDto | null = null;
  loading = true;

  profileForm: FormGroup;
  profileSaving = false;
  profileError: string | null = null;

  passwordForm: FormGroup;
  passwordSaving = false;
  passwordError: string | null = null;

  constructor() {
    this.profileForm = this.fb.group({
      fullName: ["", [Validators.required]],
      email: ["", [Validators.required, Validators.email]],
    });

    // ⚠️ שמות השדות password/confirmPassword אינם שרירותיים — passwordsMatch מחפש
    // בדיוק אותם ב-group שהוא מותקן עליו.
    this.passwordForm = this.fb.group(
      {
        currentPassword: ["", [Validators.required]],
        password: ["", [Validators.required, passwordStrengthValidator]],
        confirmPassword: ["", [Validators.required]],
      },
      { validators: [passwordsMatch, sameAsCurrentPassword] },
    );
  }

  ngOnInit(): void {
    this.auth.getMyProfile().subscribe({
      next: (profile) => {
        this.profile = profile;
        this.loading = false;

        // תלמידה לא רואה את השדות האלה ולא שולחת אותם, אבל הטופס נטען בכל מקרה:
        // ריק הוא invalid, וזה לא משנה כי כפתור השמירה חבוי אצלה ממילא.
        this.profileForm.reset({
          fullName: profile.fullName,
          email: profile.email ?? "",
        });
      },
      error: () => {
        // ה-ApiErrorInterceptor כבר הציג טוסט. כאן רק יוצאים ממצב הטעינה,
        // אחרת המסך נשאר על שלד לנצח.
        this.loading = false;
      },
    });
  }

  resetProfileForm(): void {
    if (!this.profile) return;

    this.profileError = null;
    this.profileForm.reset({
      fullName: this.profile.fullName,
      email: this.profile.email ?? "",
    });
  }

  saveProfile(): void {
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      return;
    }

    this.profileSaving = true;
    this.profileError = null;

    this.auth
      .updateMyProfile({
        fullName: this.profileForm.value.fullName,
        email: this.profileForm.value.email,
      })
      .subscribe({
        next: () => {
          this.profileSaving = false;

          // ⚠️ עדכון profile המקומי אינו מיותר: הוא מה ש-"ביטול שינויים" חוזר אליו,
          // ובלעדיו הכפתור היה משחזר את הערכים שלפני השמירה.
          if (this.profile) {
            this.profile = {
              ...this.profile,
              fullName: this.profileForm.value.fullName,
              email: this.profileForm.value.email,
            };
          }

          // markAsPristine ולא reset: הערכים בשדות נכונים ואין סיבה לצייר אותם מחדש,
          // רק להחזיר את הטופס למצב "אין מה לשמור".
          this.profileForm.markAsPristine();

          // השם בסרגל העליון כבר התעדכן — AuthService.updateMyProfile שמר את הטוקן
          // החדש ואת sg_user בתוך ה-tap.
          this.messageService.add({
            severity: "success",
            summary: "בוצע",
            detail: "פרטי החשבון עודכנו.",
          });
        },
        error: (err: { status?: number; error?: { detail?: string } }) => {
          this.profileSaving = false;

          if (err?.status === 409) {
            this.profileError =
              "כתובת המייל הזו כבר משויכת לחשבון אחר במערכת.";
            return;
          }

          this.profileError =
            err?.error?.detail || "עדכון פרטי החשבון נכשל. יש לנסות שוב.";
        },
      });
  }

  changePassword(): void {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    this.passwordSaving = true;
    this.passwordError = null;

    this.auth
      .changeMyPassword({
        currentPassword: this.passwordForm.value.currentPassword,
        newPassword: this.passwordForm.value.password,
      })
      .subscribe({
        next: () => {
          this.passwordSaving = false;

          // ⚠️ reset מלא, ולא רק pristine: השדות מחזיקים סיסמאות בטקסט מלא, ואין
          // סיבה להשאיר אותן על המסך אחרי שהפעולה הסתיימה.
          this.passwordForm.reset({
            currentPassword: "",
            password: "",
            confirmPassword: "",
          });

          this.messageService.add({
            severity: "success",
            summary: "בוצע",
            detail: "הסיסמה הוחלפה. אין צורך להתחבר מחדש.",
          });
        },
        error: (err: { error?: { detail?: string } }) => {
          this.passwordSaving = false;

          // BusinessRuleException על סיסמה נוכחית שגויה חוזר כ-detail. הוא מפורש
          // בכוונה — הקוראת כבר מחוברת כבעלת החשבון ואינה לומדת ממנו דבר שאינו שלה.
          this.passwordError =
            err?.error?.detail || "החלפת הסיסמה נכשלה. יש לנסות שוב.";
        },
      });
  }
}

/**
 * הסיסמה החדשה חייבת להיות שונה מהנוכחית — אותו כלל בדיוק שנאכף בשרת
 * (ChangeMyPasswordCommandValidator). בלעדיו הטופס נשלח, השרת דוחה, והמשתמשת
 * מקבלת שגיאה על משהו שאפשר היה לומר לה מיד.
 */
function sameAsCurrentPassword(
  group: AbstractControl,
): ValidationErrors | null {
  const current = group.get("currentPassword")?.value;
  const next = group.get("password")?.value;
  return current && next && current === next ? { sameAsCurrent: true } : null;
}
