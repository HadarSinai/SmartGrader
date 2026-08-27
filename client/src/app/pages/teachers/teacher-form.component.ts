import { CommonModule } from "@angular/common";
import { Component, OnInit, inject } from "@angular/core";
import {
  FormBuilder,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from "@angular/forms";
import { ActivatedRoute, Router } from "@angular/router";
import {
  CreateTeacherRequestDto,
  TeacherResponseDto,
  UpdateTeacherRequestDto,
} from "@models/teacher.model";
import { TeachersService } from "@services/teachers.service";
import { ConfirmationService, MessageService } from "primeng/api";
import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { ConfirmDialogModule } from "primeng/confirmdialog";
import { InputTextModule } from "primeng/inputtext";
import { PasswordModule } from "primeng/password";
import { PasswordChecklistComponent } from "../../components/password-checklist/password-checklist.component";
import { NoHebrewDirective } from "../../core/directives/no-hebrew.directive";
import { passwordStrengthValidator } from "../../core/validators/password.validator";

@Component({
  selector: "app-teacher-form",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    CardModule,
    InputTextModule,
    ButtonModule,
    ConfirmDialogModule,
    PasswordModule,
    PasswordChecklistComponent,
    NoHebrewDirective,
  ],
  providers: [ConfirmationService],
  template: `
    <section class="sg-page">
      <div class="pt-3 pb-5">
        <p-card styleClass="sg-card sg-form-card">
          <ng-template pTemplate="header">
            <div
              class="flex flex-column md:flex-row md:align-items-end md:justify-content-between gap-3 px-4 pt-4 pb-2"
            >
              <div class="sg-title">
                <div class="sg-h1">
                  {{ isEditMode ? "עריכת מורה" : "מורה חדשה" }}
                </div>
                <div class="sg-h2">
                  {{
                    isEditMode
                      ? "עדכון פרטי החשבון ואיפוס סיסמה"
                      : "יצירת חשבון התחברות למורה חדשה במערכת"
                  }}
                </div>
              </div>
            </div>
          </ng-template>

          <form class="px-4 pb-4" [formGroup]="form" (ngSubmit)="onSubmit()">
            <div class="formgrid grid">
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
                    form.get('fullName')?.invalid &&
                    form.get('fullName')?.touched
                  "
                >
                  שם מלא הוא שדה חובה
                </small>
              </div>

              <!-- שם המשתמש נקבע פעם אחת ביצירה. בעריכה הוא מוצג כטקסט ולא כשדה:
                   הוא מזהה ההתחברות, ושינוי שלו היה מנתק את המורה מהחשבון שלה. -->
              <div class="field col-12 md:col-6" *ngIf="!isEditMode">
                <label class="block font-bold mb-2" for="username"
                  >שם משתמש *</label
                >
                <input
                  pInputText
                  sgNoHebrew
                  class="w-full"
                  id="username"
                  type="text"
                  formControlName="username"
                  autocomplete="off"
                />
                <small
                  class="p-error"
                  *ngIf="
                    form.get('username')?.invalid &&
                    form.get('username')?.touched
                  "
                >
                  נדרש שם משתמש (לפחות 3 תווים, ללא רווחים)
                </small>
              </div>

              <div class="field col-12 md:col-6" *ngIf="isEditMode">
                <label class="block font-bold mb-2">שם משתמש</label>
                <div class="sg-readonly-value">
                  {{ teacher?.username || "—" }}
                </div>
                <small class="block mt-1 text-color-secondary">
                  שם המשתמש נקבע ביצירת החשבון ואינו ניתן לשינוי
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
                    form.get('email')?.hasError('required') &&
                    form.get('email')?.touched
                  "
                >
                  מייל הוא שדה חובה
                </small>
                <small
                  class="p-error"
                  *ngIf="
                    form.get('email')?.hasError('email') &&
                    form.get('email')?.touched
                  "
                >
                  כתובת המייל אינה תקינה
                </small>
                <small class="block mt-1 text-color-secondary">
                  זו הכתובת שדרכה תשוחזר הסיסמה של החשבון
                </small>
              </div>

              <div class="field col-12 md:col-6" *ngIf="!isEditMode">
                <label class="block font-bold mb-2" for="password"
                  >סיסמה *</label
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
                  *ngIf="form.get('password')?.value"
                  [password]="form.get('password')?.value"
                />
                <small
                  class="p-error"
                  *ngIf="
                    form.get('password')?.hasError('required') &&
                    form.get('password')?.touched
                  "
                >
                  נדרשת סיסמה
                </small>
              </div>
            </div>

            <div class="sg-account-error" *ngIf="formError" role="alert">
              <i class="pi pi-exclamation-circle" aria-hidden="true"></i>
              <span>{{ formError }}</span>
            </div>

            <div class="sg-form-actions">
              <p-button
                label="ביטול"
                severity="secondary"
                [outlined]="true"
                (onClick)="onCancel()"
                type="button"
              >
              </p-button>
              <p-button
                [label]="isEditMode ? 'שמירה' : 'יצירה'"
                type="submit"
                styleClass="sg-btn-primary"
                [loading]="loading"
                [disabled]="form.invalid"
              >
              </p-button>
            </div>
          </form>

          <!-- איפוס סיסמה — רק בעריכה, ובבלוק נפרד עם כפתור משלו: זו פעולה שנשמרת
               מיד ולא חלק משמירת הפרטים. -->
          <div class="px-4 pb-4" *ngIf="isEditMode">
            <div class="sg-account-section">
              <div class="sg-account-title">
                <i class="pi pi-key" aria-hidden="true"></i>
                איפוס סיסמה
              </div>

              <div class="text-color-secondary mb-3">
                הסיסמה החדשה נכנסת לתוקף מיד, וגם משחררת חשבון שננעל אחרי כמה
                ניסיונות התחברות כושלים. יש למסור אותה למורה.
              </div>

              <div class="formgrid grid">
                <div class="field col-12 md:col-6">
                  <label class="block font-bold mb-2" for="newPassword"
                    >סיסמה חדשה</label
                  >
                  <p-password
                    sgNoHebrew
                    inputId="newPassword"
                    [formControl]="newPassword"
                    [feedback]="false"
                    [toggleMask]="true"
                    autocomplete="new-password"
                    styleClass="w-full"
                    inputStyleClass="w-full"
                  />
                  <app-password-checklist
                    *ngIf="newPassword.value"
                    [password]="newPassword.value"
                  />
                  <small
                    class="p-error"
                    *ngIf="newPassword.invalid && newPassword.touched"
                  >
                    נדרשת סיסמה העומדת בכל הכללים
                  </small>
                </div>
              </div>

              <div class="sg-account-error" *ngIf="passwordError" role="alert">
                <i class="pi pi-exclamation-circle" aria-hidden="true"></i>
                <span>{{ passwordError }}</span>
              </div>

              <p-button
                label="איפוס הסיסמה"
                icon="pi pi-key"
                severity="secondary"
                [outlined]="true"
                type="button"
                [loading]="passwordLoading"
                (onClick)="confirmResetPassword()"
              >
              </p-button>
            </div>
          </div>
        </p-card>
      </div>
    </section>

    <p-confirmDialog></p-confirmDialog>
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
export class TeacherFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly teachersService = inject(TeachersService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  form!: FormGroup;
  newPassword = new FormControl("", [
    Validators.required,
    passwordStrengthValidator,
  ]);

  loading = false;
  isEditMode = false;
  teacherId: number | null = null;
  teacher: TeacherResponseDto | null = null;

  formError: string | null = null;
  passwordError: string | null = null;
  passwordLoading = false;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get("id");
    this.isEditMode = !!id;

    // הטופס נבנה לפי המצב ולא נבנה פעם אחת ומושבת בחלקו: בעריכה אין כאן שם משתמש
    // ואין סיסמה, ובקרות מושבתות היו עדיין מחזירות ערכים ל-form.value.
    this.form = this.isEditMode
      ? this.fb.group({
          fullName: ["", Validators.required],
          email: ["", [Validators.required, Validators.email]],
        })
      : this.fb.group({
          fullName: ["", Validators.required],
          username: [
            "",
            [
              Validators.required,
              Validators.minLength(3),
              Validators.maxLength(50),
              Validators.pattern(/^\S+$/),
            ],
          ],
          email: ["", [Validators.required, Validators.email]],
          password: ["", [Validators.required, passwordStrengthValidator]],
        });

    if (id) {
      this.teacherId = parseInt(id, 10);
      this.loadTeacher(this.teacherId);
    }
  }

  loadTeacher(id: number): void {
    this.loading = true;
    this.teachersService.getById(id).subscribe({
      next: (teacher: TeacherResponseDto) => {
        this.teacher = teacher;
        this.form.patchValue({
          fullName: teacher.fullName,
          email: teacher.email ?? "",
        });

        // מורה שנוצרה לפני עמודת המייל מגיעה בלי מייל. השדה ריק וחובה, ולכן הטופס
        // כבר לא תקין — מסמנים אותו כ-touched כדי שההודעה תופיע מיד ולא רק אחרי
        // שהמנהלת תיגע בשדה ותצא ממנו.
        if (!teacher.email) {
          this.form.get("email")?.markAsTouched();
        }

        this.loading = false;
      },
      error: () => {
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "טעינת פרטי המורה נכשלה",
        });
        this.loading = false;
      },
    });
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.formError = null;

    const operation = this.isEditMode
      ? this.teachersService.update(
          this.teacherId!,
          this.form.value as UpdateTeacherRequestDto,
        )
      : this.teachersService.create(this.form.value as CreateTeacherRequestDto);

    operation.subscribe({
      next: () => {
        this.messageService.add({
          severity: "success",
          summary: "בוצע",
          detail: this.isEditMode
            ? "פרטי המורה עודכנו בהצלחה"
            : "חשבון המורה נוצר בהצלחה",
        });
        this.router.navigate(["/teachers"]);
      },
      error: (err: { status?: number }) => {
        this.loading = false;
        // 409 מגיע גם על שם משתמש תפוס וגם על מייל תפוס — ההודעה מכסה את שניהם
        // במקום לנחש איזה מהם התנגש.
        this.formError =
          err?.status === 409
            ? "שם המשתמש או כתובת המייל כבר תפוסים בחשבון אחר"
            : this.isEditMode
              ? "עדכון פרטי המורה נכשל"
              : "יצירת חשבון המורה נכשלה";
      },
    });
  }

  confirmResetPassword(): void {
    if (this.newPassword.invalid || !this.teacherId) {
      this.newPassword.markAsTouched();
      return;
    }

    this.confirmationService.confirm({
      message: `לאפס את הסיסמה של "${this.teacher?.fullName ?? ""}"? הסיסמה הנוכחית שלה תפסיק לעבוד מיד.`,
      header: "אישור איפוס סיסמה",
      icon: "pi pi-exclamation-triangle",
      acceptLabel: "איפוס",
      rejectLabel: "ביטול",
      accept: () => this.resetPassword(),
    });
  }

  resetPassword(): void {
    this.passwordLoading = true;
    this.passwordError = null;

    this.teachersService
      .resetPassword(this.teacherId!, { newPassword: this.newPassword.value })
      .subscribe({
        next: () => {
          this.passwordLoading = false;
          this.newPassword.reset("");
          this.messageService.add({
            severity: "success",
            summary: "בוצע",
            detail: "הסיסמה אופסה בהצלחה. יש למסור אותה למורה.",
          });
        },
        error: () => {
          this.passwordLoading = false;
          this.passwordError = "איפוס הסיסמה נכשל, יש לנסות שוב";
        },
      });
  }

  onCancel(): void {
    if (this.form.dirty || this.newPassword.dirty) {
      this.confirmationService.confirm({
        message: "יש לך שינויים שלא נשמרו. לצאת בכל זאת?",
        header: "שינויים שלא נשמרו",
        icon: "pi pi-exclamation-triangle",
        acceptLabel: "יציאה",
        rejectLabel: "ביטול",
        accept: () => this.router.navigate(["/teachers"]),
      });
      return;
    }
    this.router.navigate(["/teachers"]);
  }
}
