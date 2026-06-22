import { CommonModule } from "@angular/common";
import { Component, OnInit, inject } from "@angular/core";
import {
  FormArray,
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from "@angular/forms";
import { ActivatedRoute, Router } from "@angular/router";
import {
  AssignmentResponseDto,
  CreateAssignmentRequestDto,
  TestCaseDto,
  UpdateAssignmentRequestDto,
} from "@models/assignment.model";
import { AssignmentsService } from "@services/assignments.service";
import { MessageService } from "primeng/api";
import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";
import { CheckboxModule } from "primeng/checkbox";
import { InputNumberModule } from "primeng/inputnumber";
import { InputTextModule } from "primeng/inputtext";
import { InputTextareaModule } from "primeng/inputtextarea";

@Component({
  selector: "app-assignment-form",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    CardModule,
    InputTextModule,
    InputTextareaModule,
    InputNumberModule,
    CheckboxModule,
    ButtonModule,
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
                <div class="sg-h1">
                  {{ isEditMode ? "עריכת תרגיל" : "תרגיל חדש" }}
                </div>
                <div class="sg-h2">הגדרת תרגיל ומקרי בדיקה</div>
              </div>
            </div>
          </ng-template>

          <form class="px-4 pb-4" [formGroup]="form" (ngSubmit)="onSubmit()">
            <div class="formgrid grid">
              <div class="field col-12 md:col-6">
                <label class="block font-bold mb-2" for="title">כותרת *</label>
                <input
                  pInputText
                  class="w-full"
                  id="title"
                  formControlName="title"
                  placeholder="לדוגמה: מיון מערכים"
                />
              </div>

              <div class="field col-12 md:col-6">
                <label class="block font-bold mb-2" for="bonusValue"
                  >בונוס</label
                >
                <div class="flex align-items-center gap-3 flex-wrap">
                  <div class="flex align-items-center gap-2">
                    <p-checkbox
                      inputId="isBonus"
                      formControlName="isBonus"
                      [binary]="true"
                    ></p-checkbox>
                    <label for="isBonus" class="font-semibold"
                      >תרגיל בונוס</label
                    >
                  </div>

                  <p-inputNumber
                    *ngIf="form.get('isBonus')?.value"
                    inputId="bonusValue"
                    styleClass="w-full"
                    formControlName="bonusValue"
                    [minFractionDigits]="0"
                    [maxFractionDigits]="2"
                  >
                  </p-inputNumber>
                </div>
              </div>

              <div class="field col-12">
                <label class="block font-bold mb-2" for="description"
                  >תיאור</label
                >
                <textarea
                  pInputTextarea
                  class="w-full"
                  id="description"
                  formControlName="description"
                  rows="4"
                  placeholder="הסבר קצר לסטודנטים"
                ></textarea>
              </div>

              <div class="field col-12">
                <div
                  class="flex align-items-center justify-content-between gap-2 flex-wrap"
                >
                  <div class="font-bold">מקרי בדיקה</div>
                  <p-button
                    label="הוספת מקרה"
                    icon="pi pi-plus"
                    [text]="true"
                    (onClick)="addTestCase()"
                    type="button"
                  ></p-button>
                </div>

                <div formArrayName="tests" class="mt-3 flex flex-column gap-3">
                  <div
                    *ngFor="let test of tests.controls; let i = index"
                    [formGroupName]="i"
                    class="p-3 border-1 border-round-xl"
                    style="border-color: var(--app-border); background: rgba(239,232,221,0.40)"
                  >
                    <div
                      class="flex align-items-center justify-content-between gap-2 flex-wrap mb-3"
                    >
                      <div class="font-bold text-color">מקרה {{ i + 1 }}</div>
                      <p-button
                        icon="pi pi-trash"
                        severity="danger"
                        [text]="true"
                        (onClick)="removeTestCase(i)"
                        type="button"
                      ></p-button>
                    </div>

                    <div class="grid">
                      <div class="col-12 md:col-6">
                        <label class="block font-bold mb-2">קלט</label>
                        <textarea
                          pInputTextarea
                          class="w-full"
                          formControlName="input"
                          rows="2"
                          placeholder="הקלידי קלט"
                        ></textarea>
                      </div>
                      <div class="col-12 md:col-6">
                        <label class="block font-bold mb-2">פלט צפוי</label>
                        <textarea
                          pInputTextarea
                          class="w-full"
                          formControlName="expected"
                          rows="2"
                          placeholder="הקלידי פלט צפוי"
                        ></textarea>
                      </div>
                    </div>
                  </div>

                  <div
                    *ngIf="tests.length === 0"
                    class="text-color-secondary p-3 border-1 border-round-xl"
                    style="border-color: var(--app-border);"
                  >
                    עדיין אין מקרי בדיקה. מומלץ להוסיף לפחות אחד.
                  </div>
                </div>
              </div>
            </div>

            <div class="sg-form-actions">
              <p-button
                label="ביטול"
                severity="secondary"
                [outlined]="true"
                (onClick)="onCancel()"
                type="button"
              ></p-button>
              <p-button
                [label]="isEditMode ? 'שמירה' : 'יצירה'"
                type="submit"
                styleClass="sg-btn-primary"
                [loading]="loading"
                [disabled]="form.invalid"
              ></p-button>
            </div>
          </form>
        </p-card>
      </div>
    </section>
  `,
  styles: [],
})
export class AssignmentFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly assignmentsService = inject(AssignmentsService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly messageService = inject(MessageService);

  form: FormGroup;
  loading = false;
  isEditMode = false;
  lessonId!: number;
  assignmentId: number | null = null;

  constructor() {
    this.form = this.fb.group({
      title: ["", Validators.required],
      description: [""],
      isBonus: [false],
      bonusValue: [0],
      tests: this.fb.array([]),
    });
  }

  get tests(): FormArray {
    return this.form.get("tests") as FormArray;
  }

  ngOnInit(): void {
    const lessonIdParam = this.route.snapshot.paramMap.get("lessonId");
    const assignmentIdParam = this.route.snapshot.paramMap.get("assignmentId");

    if (lessonIdParam) {
      this.lessonId = parseInt(lessonIdParam, 10);
    }

    if (assignmentIdParam) {
      this.isEditMode = true;
      this.assignmentId = parseInt(assignmentIdParam, 10);
      this.loadAssignment(this.lessonId, this.assignmentId);
    }
  }

  loadAssignment(lessonId: number, assignmentId: number): void {
    this.loading = true;
    this.assignmentsService.getById(lessonId, assignmentId).subscribe({
      next: (assignment: AssignmentResponseDto) => {
        this.form.patchValue({
          title: assignment.title,
          description: assignment.description,
          isBonus: assignment.isBonus,
          bonusValue: assignment.bonusValue,
        });

        if (assignment.tests) {
          assignment.tests.forEach((test) => {
            this.tests.push(this.createTestCaseGroup(test));
          });
        }

        this.loading = false;
      },
      error: (_error: unknown) => {
        this.messageService.add({
          severity: "error",
          summary: "Error",
          detail: "Failed to load assignment",
        });
        this.loading = false;
      },
    });
  }

  createTestCaseGroup(testCase?: TestCaseDto): FormGroup {
    return this.fb.group({
      input: [testCase?.input || ""],
      expected: [testCase?.expected || ""],
    });
  }

  addTestCase(): void {
    this.tests.push(this.createTestCaseGroup());
  }

  removeTestCase(index: number): void {
    this.tests.removeAt(index);
  }

  onSubmit(): void {
    if (this.form.invalid) {
      return;
    }

    this.loading = true;
    const formValue = this.form.value;
    const request = {
      title: formValue.title,
      description: formValue.description,
      isBonus: formValue.isBonus,
      bonusValue: formValue.bonusValue,
      tests: formValue.tests,
    };

    const operation = this.isEditMode
      ? this.assignmentsService.update(
          this.lessonId,
          this.assignmentId!,
          request as UpdateAssignmentRequestDto,
        )
      : this.assignmentsService.create(
          this.lessonId,
          request as CreateAssignmentRequestDto,
        );

    operation.subscribe({
      next: () => {
        this.messageService.add({
          severity: "success",
          summary: "Success",
          detail: `Assignment ${this.isEditMode ? "updated" : "created"} successfully`,
        });
        this.router.navigate(["/lessons", this.lessonId, "assignments"]);
      },
      error: (_error: unknown) => {
        this.messageService.add({
          severity: "error",
          summary: "Error",
          detail: `Failed to ${this.isEditMode ? "update" : "create"} assignment`,
        });
        this.loading = false;
      },
    });
  }

  onCancel(): void {
    this.router.navigate(["/lessons", this.lessonId, "assignments"]);
  }
}
