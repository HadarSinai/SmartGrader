# Glossary

> SmartGrader · Version 1.0 · Last updated 2026-08-26 · Status: as-built

| Version | Date | Change |
|---|---|---|
| 1.0 | 2026-08-26 | First edition. |

**What this document answers:** what a product term means, and which identifier in the code carries it.

**Why it exists.** This is the enabling document for the whole set. The product is Hebrew; the codebase
is English; the specification is English. Without one agreed mapping, every other document fills up
with type names — which is exactly the failure of the old set, where a teacher's goal was written as
*"a correct `AssignmentResponseDto`"*.

**The rule it enables:** outside this file, requirements use the **term**, never the type name.
Type names belong in a code-anchor column, where they are pointing at code on purpose.

The `Code identifier` column is machine-checked against the compiled assemblies. Rename `MethodName`
and CI fails here. The `Meaning` column is prose and is not asserted.

---

## Domain terms

<!-- gen:identifiers Domain -->

| Hebrew term | Code identifier | Meaning |
|---|---|---|
| משתמש/ת | `User` | Any login. Teacher, student and admin are one table with a role. |
| תפקיד | `UserRole` | Teacher, Student or Admin. |
| מורה | `User.Role` | There is **no** `Teacher` entity — a teacher is a user whose role is Teacher. |
| תלמיד/ה | `Student` | A person in a class. Distinct from her login, which may not exist. |
| כיתה | `SchoolClass` | A class in one academic year. Has no owner. |
| שנת לימודים | `SchoolClass.AcademicYear` | The Hebrew year as a number — 5786 = תשפ"ו. |
| כיתה בארכיון | `SchoolClass.IsArchived` | Year rollover. Locks every submission of every student in it. |
| קורס | `Course` | A named course belonging to one teacher. |
| שיעור | `Lesson` | One lesson on one date, inside a course, assigned to one or more classes. |
| נושא השיעור | `Lesson.Subject` | Free text, not an entity. |
| תאריך השיעור | `Lesson.LessonDate` | Stored as a `DateTime`; entered and displayed as a Hebrew date. |
| תרגיל | `Assignment` | One exercise inside a lesson. |
| שם המתודה | `Assignment.MethodName` | The method the runner calls, in the method-based grading modes. |
| אופן בדיקה | `GradingMode` | Whole program, single method, or multi-file project. |
| תרגיל בונוס | `Assignment.IsBonus` | Its ceiling exceeds 100. |
| ערך הבונוס | `Assignment.BonusValue` | By how much the ceiling exceeds 100. |
| תקרת ציון | `Assignment.MaxScore` | Derived, never stored: 100, or 100 + the bonus value. |
| הקצאה למקרי בדיקה | `Assignment.TestsAllocation` | How many of the points go to the tests. 0 is legal. |
| סף הגשה חוזרת | `Assignment.RetryThreshold` | Below this score the student may submit again. Default 85. |
| מקרה בדיקה | `TestCase` | One input and its expected output. |
| מקרה דוגמה | `TestCase.IsSample` | The only kind a student ever sees. Defaults to false — fail closed. |
| מקרה ליבה | `TestCase.IsCore` | Tests the central thing, as opposed to an edge case. Defaults to true. |
| דרישה מבנית | `StructuralRule` | A requirement about the shape of the code, checked by Roslyn. |
| סוג הדרישה | `RuleKind` | Must use / must not use / at least / at most. |
| מבנה בקוד | `CodeConstruct` | The catalog of structures a requirement may name. |
| סף | `StructuralRule.Threshold` | The count for *at least* / *at most*. |
| חומרת הדרישה | `RuleSeverity` | What happens when it is not met. |
| דרישה חוסמת | `RuleSeverity.Blocking` | Rejection, not a low grade. No score is produced. Carries no points. |
| דרישה מנוקדת | `RuleSeverity.Scored` | Failing loses its points in full. No partial credit. |
| המלצה | `RuleSeverity.Advisory` | A note in the feedback only. |
| נקודות הדרישה | `StructuralRule.Points` | Only meaningful for a scored requirement. |
| הפתרון לדוגמה | `ReferenceSolutionFile` | The teacher's known-good solution. **The full answer** — never sent to a student. |
| קובץ נדרש | `ExpectedFile` | A file name a multi-file submission must contain. |
| הגשה | `Submission` | The current state of one student's work on one assignment. One row, forever. |
| קובץ בהגשה | `SubmissionFile` | One file of a multi-file submission. |
| ניסיון | `SubmissionAttempt` | A finished attempt, archived before the submission was reset. |
| מספר הניסיון | `Submission.AttemptNumber` | From 1. Only the last attempt counts. |
| מצב ההגשה | `SubmissionStatus` | Seven states — see the domain model. |
| ציון ההגשה | `Submission.Score` | `null` until graded, and `null` again after a resubmit. |
| אישור הגשה נוספת | `Submission.HasUnusedExtraAttempt` | A one-shot teacher grant. Overrides the retry threshold, never a lock. |
| דריסת ציון | `Submission.OverrideScore` | The teacher sets a score by hand. Requires a written reason. |
| פירוק הציון | `ScoreBreakdown` | Tests · requirements · total, so a number can be argued with. |
| נקודות הבדיקות | `ScoreBreakdown.TestPoints` | Points earned on the test cases. |
| נקודות הדרישות | `ScoreBreakdown.RulePoints` | Points earned on the scored requirements. |
| תוצאת מקרה בדיקה | `TestCaseResult` | One test's input, expected, actual and verdict. |
| תוצאת דרישה | `StructuralRuleResult` | One requirement's verdict, the count found, and the line numbers. |
| ציון סופי לשיעור | `LessonResult.FinalScore` | What the student actually receives for the lesson. |
| ציון מחושב | `LessonResult.ComputedScore` | What the system derived. Kept even when overridden. |
| שיעור מסוכם | `LessonResult.IsComplete` | Finalised for that student. Locks her submissions in that lesson. |
| מחשבון הציון | `ScoreCalculator` | Turns test and requirement results into a score. A pure function. |
| מחשבון ציון השיעור | `LessonScoreCalculator` | Averages the graded assignments into a lesson score. A pure function. |
| קישור לאיפוס סיסמה | `PasswordResetToken` | Stores a hash of the token, never the token. |
| נעילת חשבון | `User.LockoutEndsAt` | After five consecutive failures, for fifteen minutes. |
| רשומת יומן | `Log` | One system event worth keeping. |

<!-- /gen -->

## Authorization terms

These live in the application layer, not the domain — they are how a rule is *enforced*, not what a
thing *is*. [permissions.md](permissions.md) explains the three mechanisms.

<!-- gen:identifiers Application -->

| Hebrew term | Code identifier | Meaning |
|---|---|---|
| בעלות על שיעור | `LessonAccess` | The ownership check for a lesson and everything nested under it. |
| היקף התלמידות | `StudentScope` | Which students a given teacher may see, derived through her lessons' classes. |
| הסתרת התשובה | `TestVisibility` | Removes hidden test cases and the reference solution before a DTO reaches a student. |
| נעילת הגשה | `SubmissionLock` | A finalised lesson or an archived class. Overrides every other rule, including a teacher's grant. |

<!-- /gen -->

## Terms with no single identifier

Real product terms that no one symbol carries. Listed so they are not invented twice.

| Hebrew term | Meaning | Where it lives |
|---|---|---|
| רובריקה | The split of an assignment's ceiling between tests and scored requirements | `TestsAllocation` + the sum of `StructuralRule.Points` |
| ניקוד יחסי | Test points as `allocation × passed ÷ total` | `ScoreCalculator` |
| שער מקרי הליבה | All core tests must pass before any test points are awarded | `ScoreCalculator`, `ScoreBreakdown.AllCorePassed` |
| הגבלת קצב | The minimum gap between two attempts | `Submission.MinResubmitInterval` |
| סיום שנה | Archiving every active class at once | `POST /api/classes/finish-year` — admin only |
| המסע שלי | The student area | the `/my/*` routes |
| סיגנל כיתתי | A pattern across several students in a window, computed on demand | `ClassSignalDetector`; **there is no notification entity** |
