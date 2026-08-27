import { CommonModule } from "@angular/common";
import { Component, OnInit, inject } from "@angular/core";
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from "@angular/forms";
import { ActivatedRoute, Router } from "@angular/router";
import { ConfirmationService, MessageService } from "primeng/api";
import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { ConfirmDialogModule } from "primeng/confirmdialog";
import { DropdownModule } from "primeng/dropdown";
import { InputTextModule } from "primeng/inputtext";

import { ClassesService } from "@services/classes.service";
import {
  getCurrentHebrewYear,
  hebrewYearToGematria,
} from "../../core/utils/hebrew-year";

@Component({
  selector: "app-class-form",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    CardModule,
    InputTextModule,
    DropdownModule,
    ButtonModule,
    ConfirmDialogModule,
  ],
  providers: [ConfirmationService],
  templateUrl: "./class-form.component.html",
})
export class ClassFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly classesService = inject(ClassesService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  form: FormGroup;
  loading = false;
  isEditMode = false;
  classId: number | null = null;

  yearOptions: { label: string; value: number }[] = [];

  constructor() {
    const currentYear = getCurrentHebrewYear();

    // השנה שעברה, הנוכחית ושתיים קדימה
    this.yearOptions = [-1, 0, 1, 2].map((offset) => {
      const year = currentYear + offset;
      return { label: `${hebrewYearToGematria(year)} (${year})`, value: year };
    });

    this.form = this.fb.group({
      name: ["", [Validators.required, Validators.maxLength(50)]],
      academicYear: [currentYear, Validators.required],
    });
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get("id");
    if (id) {
      this.isEditMode = true;
      this.classId = parseInt(id, 10);
      this.loadClass(this.classId);
    }
  }

  loadClass(id: number): void {
    this.loading = true;
    this.classesService.getById(id).subscribe({
      next: (schoolClass) => {
        // שנה שלא ברשימת האפשרויות (כיתה ותיקה) — מוסיפים אותה
        if (!this.yearOptions.some((o) => o.value === schoolClass.academicYear)) {
          this.yearOptions = [
            {
              label: `${schoolClass.academicYearHebrew} (${schoolClass.academicYear})`,
              value: schoolClass.academicYear,
            },
            ...this.yearOptions,
          ];
        }

        this.form.patchValue({
          name: schoolClass.name,
          academicYear: schoolClass.academicYear,
        });
        this.loading = false;
      },
      error: () => {
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail: "טעינת הכיתה נכשלה",
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
    const request = this.form.value;

    const operation = this.isEditMode
      ? this.classesService.update(this.classId!, request)
      : this.classesService.create(request);

    operation.subscribe({
      next: () => {
        this.messageService.add({
          severity: "success",
          summary: "בוצע",
          detail: this.isEditMode
            ? "הכיתה עודכנה בהצלחה"
            : "הכיתה נוצרה בהצלחה",
        });
        this.router.navigate(["/classes"]);
      },
      error: (err: { status?: number }) => {
        this.messageService.add({
          severity: "error",
          summary: "שגיאה",
          detail:
            err?.status === 409
              ? "כיתה בשם זה כבר קיימת בשנה זו"
              : this.isEditMode
                ? "עדכון הכיתה נכשל"
                : "יצירת הכיתה נכשלה",
        });
        this.loading = false;
      },
    });
  }

  onCancel(): void {
    if (this.form.dirty) {
      this.confirmationService.confirm({
        message: "יש לך שינויים שלא נשמרו. לצאת בכל זאת?",
        header: "שינויים שלא נשמרו",
        icon: "pi pi-exclamation-triangle",
        acceptLabel: "יציאה",
        rejectLabel: "ביטול",
        accept: () => this.router.navigate(["/classes"]),
      });
      return;
    }
    this.router.navigate(["/classes"]);
  }
}
