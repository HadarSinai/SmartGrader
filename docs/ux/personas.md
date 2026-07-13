# SmartGrader — UX Personas

These two personas ground every JTBD, journey map, and flow spec produced for the SmartGrader client
screens (`docs/ux/*.md`). Both are real, direct users of the Angular client — this is not an
admin-only tool.

---

## Persona 1: The Teacher (primary / admin user)

- **Who**: A CS/programming teacher running a classroom or after-school coding lesson. Sets up lessons,
  defines coding assignments with test cases, manages the student roster, reviews AI-graded submissions,
  and finalizes lesson scores.
- **Skill level with similar tools**: Comfortable with basic web apps and spreadsheets; not necessarily
  technical beyond reading code snippets to sanity-check AI feedback. Not a developer.
- **Device**: Primarily desktop/laptop in a classroom or at a desk while prepping lessons; occasionally
  tablet.
- **Context**: Two distinct modes —
  1. **Prep time** (before/after class, unhurried): creating lessons, writing assignments and test cases,
     adding students.
  2. **In-class / grading time** (time-pressured, often mid-lesson): reviewing submissions as they come
     in, checking AI feedback, finalizing a lesson's results before moving on.
- **Success metric**: A lesson's assignments are graded and every student has a finalized score with
  minimal manual intervention, in time for the next lesson.
- **Pain points (today)**:
  - Uncertain whether a screen holds all the fields she needs before saving (e.g., missing fields,
    unclear which are required).
  - No confirmation before destructive actions is unsettling for content she spent time creating
    (assignments with test cases especially).
  - Dated, inconsistent visuals across pages reduce trust that the tool is "production ready" for
    daily classroom use.
  - No way today to view or finalize a `LessonResult` at all — the feature exists on the backend but
    has zero UI, so she can't close out a lesson from the app.
- **Frequency**: Daily to weekly, depending on teaching schedule.

---

## Persona 2: The Student

- **Who**: A student in the class, ranging from beginner to intermediate programmer. Submits code
  directly through the client for a given assignment, then waits for AI grading.
- **Skill level with similar tools**: Comfortable typing code but is a novice at reading tooling status
  (spinners, error states) and English-only technical error messages.
- **Device**: Laptop or classroom desktop, sometimes personal device at home for resubmission.
- **Context**: Submitting code under mild time pressure during a lesson, or resubmitting after a
  compile/AI failure. Checks back to see if grading finished and what the feedback says.
- **Success metric**: Understands clearly whether their submission passed, and if not, what to fix —
  without needing to ask the teacher.
- **Pain points (today)**:
  - AI grading is asynchronous (`PendingAi` → `ProcessingAi` → `Done`/`AiFailed`/`CompilationFailed`);
    without a clear in-progress state the student doesn't know if the page is broken or just working.
  - Compile errors vs. AI-evaluation feedback are different failure modes but risk being shown
    similarly, causing confusion about what actually needs fixing.
  - No visibility into their own finalized lesson score (`LessonResult`) anywhere in the client today.
- **Frequency**: Multiple times per lesson (one submission per assignment, plus resubmissions).

---

## How These Personas Are Used

- **Lessons, Assignments, Students** feature areas: Teacher-only persona (CRUD/admin surface).
- **Submissions** feature area: Both personas — Student creates/resubmits, Teacher reviews.
- **LessonResults** feature area (new): Both personas — Teacher finalizes a score, Student views their
  own result.

Every `docs/ux/{feature}-jtbd.md` and `docs/ux/{feature}-journey.md` file must state explicitly which
of these two personas (or both) it is written for.
