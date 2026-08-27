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
  template: `
    <section class="sg-page">
      <div class="pt-3 pb-5">
        <p-card styleClass="sg-card sg-form-card">
          <ng-template pTemplate="header">
            <div
              class="flex flex-column md:flex-row md:align-items-end md:justify-content-between gap-3 px-4 pt-4 pb-2"
            >
              <div class="sg-title">
                <div class="sg-h1">החשבון שלי</div>
                <div class="sg-h2">
                  {{
                    auth.isStudent()
                      ? "החלפת הסיסמה של החשבון"
                      : "עדכון פרטי החשבון והחלפת הסיסמה"
                  }}
                </div>
              </div>
            </div>
          </ng-template>

          <div class="px-4 pb-4" *ngIf="loading">
            <p-skeleton height="2.5rem" styleClass="mb-3"></p-skeleton>
            <p-skeleton height="2.5rem" styleClass="mb-3"></p-skeleton>
            <p-skeleton height="2.5rem"></p-skeleton>
          </div>

          <ng-container *ngIf="!loading && profile">
            <!-- ── פרטי החשבון ── -->
            <form class="px-4 pb-4" [formGroup]="profileForm" (ngSubmit)="saveProfile()">
              <div class="formgrid grid">
                <!-- שם המשתמש מוצג כטקסט ולא כשדה: הוא מזהה ההתחברות, ואין לו
                     SetUsername בשרת. שינוי שלו היה מנתק את המשתמשת מהחשבון שלה. -->
                <div class="field col-12 md:col-6">
                  <label class="block font-bold mb-2">שם משתמש</label>
                  <div class="sg-readonly-value">{{ profile.username }}</div>
                  <small class="block mt-1 text-color-secondary">
                    שם המשתמש נקבע ביצירת החשבון ואינו ניתן לשינוי
                  </small>
                </div>

                <!-- לתלמידה מוצג השם לקריאה בלבד, כדי שהמסך לא ייראה ריק ותדע
                     למי לפנות כדי לתקן אותו. -->
                <div class="field col-12 md:col-6" *ngIf="auth.isStudent()">
                  <label class="block font-bold mb-2">שם מלא</label>
                  <div class="sg-readonly-value">{{ profile.fullName }}</div>
                  <small class="block mt-1 text-color-secondary">
                    השם מתוחזק בידי המורה שלך. אם הוא שגוי, יש לפנות אליה.
                  </small>
                </div>

                <ng-container *ngIf="!auth.isStudent()">
                  <div class="field col-12 md:col-6">
                    <label class="block font-bold mb-2" for="fullName"
                      >שם מלא *</label
                    >
                    <input
                      pInputText
                      class="w-full"
                      id="fullName"
                      formControlName="fullName"
                      placeholder="לדוגמה: מרים לוי"
                    />
                    <small
                      class="p-error"
                      *ngIf="
                        profileForm.get('fullName')?.invalid &&
                        profileForm.get('fullName')?.touched
                      "
                    >
                      שם מלא הוא שדה חובה
                    </small>
                  </div>

                  <div class="field col-12 md:col-6">
                    <label class="block font-bold mb-2" for="email">מייל *</label>
                    <input
                      pInputText
                      sgNoHebrew
                      class="w-full"
                      id="email"
                      type="email"
                      formControlName="email"
                      autocomplete="off"
                      placeholder="teacher@example.com"
                    />
                    <small
                      class="p-error"
                      *ngIf="
                        profileForm.get('email')?.hasError('required') &&
                        profileForm.get('email')?.touched
                      "
                    >
                      מייל הוא שדה חובה
                    </small>
                    <small
                      class="p-error"
                      *ngIf="
                        profileForm.get('email')?.hasError('email') &&
                        profileForm.get('email')?.touched
                      "
                    >
                      כתובת המייל אינה תקינה
                    </small>
                    <small class="block mt-1 text-color-secondary">
                      זו הכתובת שדרכה תשוחזר הסיסמה של החשבון
                    </small>
                  </div>
                </ng-container>
              </div>

              <div class="sg-account-error" *ngIf="profileError" role="alert">
                <i class="pi pi-exclamation-circle" aria-hidden="true"></i>
                <span>{{ profileError }}</span>
              </div>

              <div class="sg-form-actions" *ngIf="!auth.isStudent()">
                <p-button
                  label="ביטול שינויים"
                  severity="secondary"
                  [outlined]="true"
                  type="button"
                  [disabled]="profileForm.pristine || profileSaving"
                  (onClick)="resetProfileForm()"
                >
                </p-button>
                <p-button
                  label="שמירה"
                  type="submit"
                  styleClass="sg-btn-primary"
                  [loading]="profileSaving"
                  [disabled]="profileForm.invalid || profileForm.pristine"
                >
                </p-button>
              </div>
            </form>

            <!-- ── החלפת סיסמה — בלוק נפרד עם כפתור משלו, כמו במסך עריכת מורה:
                 זו פעולה שנשמרת מיד ואינה חלק משמירת הפרטים. ── -->
            <div class="px-4 pb-4">
              <form
                class="sg-account-section"
                [formGroup]="passwordForm"
                (ngSubmit)="changePassword()"
              >
                <div class="sg-account-title">
                  <i class="pi pi-key" aria-hidden="true"></i>
                  החלפת סיסמה
                </div>

                <div class="text-color-secondary mb-3">
                  הסיסמה החדשה נכנסת לתוקף מיד. אין צורך להתחבר מחדש.
                </div>

                <div class="formgrid grid">
                  <div class="field col-12 md:col-4">
                    <label class="block font-bold mb-2" for="currentPassword"
                      >הסיסמה הנוכחית *</label
                    >
                    <p-password
                      sgNoHebrew
                      inputId="currentPassword"
                      formControlName="currentPassword"
                      [feedback]="false"
                      [toggleMask]="true"
                      autocomplete="current-password"
                      styleClass="w-full"
                      inputStyleClass="w-full"
                    />
                    <small
                      class="p-error"
                      *ngIf="
                        passwordForm.get('currentPassword')?.invalid &&
                        passwordForm.get('currentPassword')?.touched
                      "
                    >
                      נדרשת הסיסמה הנוכחית
                    </small>
                    <small class="block mt-1 text-color-secondary">
                      נדרשת כדי שמי שמתיישבת מול מחשב שנשאר מחובר לא תוכל להחליף
                      את הסיסמה שלך
                    </small>
                  </div>

                  <div class="field col-12 md:col-4">
                    <label class="block font-bold mb-2" for="password"
                      >סיסמה חדשה *</label
                    >
                    <p-password
                      sgNoHebrew
                      inputId="password"
                      formControlName="password"
                      [feedback]="false"
                      [toggleMask]="true"
                      autocomplete="new-password"
                      styleClass="w-full"
                      inputStyleClass="w-full"
                    />
                    <app-password-checklist
                      *ngIf="passwordForm.get('password')?.value"
                      [password]="passwordForm.get('password')?.value"
                    />
                    <small
                      class="p-error"
                      *ngIf="
                        passwordForm.get('password')?.hasError('required') &&
                        passwordForm.get('password')?.touched
                      "
                    >
                      נדרשת סיסמה
                    </small>
                    <small
                      class="p-error"
                      *ngIf="
                        passwordForm.hasError('sameAsCurrent') &&
                        passwordForm.get('password')?.touched
                      "
                    >
                      הסיסמה החדשה זהה לנוכחית
                    </small>
                  </div>

                  <div class="field col-12 md:col-4">
                    <label class="block font-bold mb-2" for="confirmPassword"
                      >אימות הסיסמה החדשה *</label
                    >
                    <p-password
                      sgNoHebrew
                      inputId="confirmPassword"
                      formControlName="confirmPassword"
                      [feedback]="false"
                      [toggleMask]="true"
                      autocomplete="new-password"
                      styleClass="w-full"
                      inputStyleClass="w-full"
                    />
                    <small
                      class="p-error"
                      *ngIf="
                        passwordForm
                          .get('confirmPassword')
                          ?.hasError('required') &&
                        passwordForm.get('confirmPassword')?.touched
                      "
                    >
                      נדרש אימות של הסיסמה
                    </small>
                    <small
                      class="p-error"
                      *ngIf="
                        passwordForm.hasError('passwordsMismatch') &&
                        passwordForm.get('confirmPassword')?.touched
                      "
                    >
                      הסיסמאות אינן תואמות
                    </small>
                  </div>
                </div>

                <div class="sg-account-error" *ngIf="passwordError" role="alert">
                  <i class="pi pi-exclamation-circle" aria-hidden="true"></i>
                  <span>{{ passwordError }}</span>
                </div>

                <p-button
                  label="החלפת הסיסמה"
                  icon="pi pi-key"
                  severity="secondary"
                  [outlined]="true"
                  type="submit"
                  [loading]="passwordSaving"
                  [disabled]="passwordForm.invalid"
                >
                </p-button>
              </form>
            </div>
          </ng-container>
        </p-card>
      </div>
    </section>
  `,
  styles: [
    `
      /* מקטע פרטי הכניסה המשותף לשלושת הטפסים יושב ב-styles.css. */

      :host ::ng-deep .p-password {
        width: 100%;
      }
    `,
  ],
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
