namespace SmartGrader.Application.Dtos.Assignments
{
            public class AssignmentResponseDto
        {
            public int Id { get; set; }
            public int LessonId { get; set; }
            public string? Title { get; set; }
            public string? Description { get; set; }
            public bool IsBonus { get; set; }
            public double BonusValue { get; set; }
            public string MethodName { get; init; } = "";
            public string GradingMode { get; set; } = "";
            public DateTime CreatedAt { get; set; }
            public int SubmissionsCount { get; set; }
           

            public List<TestCaseDto> Tests { get; set; } = new();
            public List<ExpectedFileDto> ExpectedFiles { get; set; } = new();

            /// <summary>
            /// ⚠️ הפתרון המלא לתרגיל. מרוקן בשרת עבור תלמידה ב-<c>TestVisibility.Redact</c>
            /// לפני שה-DTO עוזב את ה-handler — אין להסתמך על הסתרה בלקוח.
            /// </summary>
            public List<ReferenceSolutionFileDto> ReferenceSolution { get; set; } = new();

            /// <summary>
            /// הדרישות המבניות. ⚠️ בניגוד ל-<c>Tests</c> וְל-<c>ReferenceSolution</c>,
            /// <b>אין כאן מה להסתיר מהתלמידה</b>: הדרישה נכתבה בניסוח המטלה מלכתחילה
            /// ("חובה רקורסיה"), ובלעדיה היא לא יודעת מה נדרש ממנה.
            /// </summary>
            public List<StructuralRuleDto> StructuralRules { get; set; } = new();

            /// <summary>כמה מ-100 הנקודות מוקצות למקרי הבדיקה.</summary>
            public int TestsAllocation { get; set; }

            /// <summary>כמה נקודות מחולקות בין הדרישות המנוקדות. משלים ל-100.</summary>
            public int RulesAllocation { get; set; }

            /// <summary>הציון שמתחתיו ההגשה נשארת פתוחה להגשה חוזרת.</summary>
            public int RetryThreshold { get; set; }
    }

    }
