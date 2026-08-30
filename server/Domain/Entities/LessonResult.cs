namespace SmartGrader.Domain.Entities
{
    public class LessonResult
    {
        public int Id { get; private set; }
        public int StudentId { get; private set; }
        public int LessonId { get; private set; }
        public double? FinalScore { get; private set; }

        /// <summary>
        /// הציון שהמערכת גזרה מההגשות בשיעור ברגע הסיכום.
        /// <para>
        /// ⚠️ נשמר <b>גם</b> כשהמורה דרסה, וזו כל הנקודה: בלעדיו אי אפשר לדעת בדיעבד
        /// ממה חרגו, ואי אפשר להבחין בין ציון מחושב לציון מוקלד.
        /// </para>
        /// <para><c>null</c> כשאף תרגיל לא נבדק — אז אין ציון מחושב כלל.</para>
        /// </summary>
        public double? ComputedScore { get; private set; }

        public bool IsComplete { get; private set; }
        public DateTime? CalculatedAt { get; private set; } = DateTime.UtcNow;

        // ── דריסת הציון הסופי בידי המורה ──
        // אותו תקן שכבר נהוג רמה אחת למטה ב-Submission.OverrideScore: סיבה חובה, ורישום
        // של מי ומתי. עד כה דריסת ציון של הגשה בודדת הייתה מעשה מתועד, ודריסת הציון הסופי
        // של שיעור שלם — שהוא הציון שהתלמידה באמת מקבלת — לא הייתה מתועדת כלל.
        public int? FinalScoreOverriddenByUserId { get; private set; }
        public DateTime? FinalScoreOverriddenAt { get; private set; }
        public string? FinalScoreOverrideReason { get; private set; }

        /// <summary>האם הציון הסופי נקבע ידנית ולא התקבל מהחישוב.</summary>
        public bool IsFinalScoreOverridden => FinalScoreOverriddenByUserId.HasValue;

        protected LessonResult() { }
        public static LessonResult Create(int studentId, int lessonId)
        {
            if (studentId <= 0) throw new ArgumentException("Invalid student id.", nameof(studentId));
            if (lessonId <= 0) throw new ArgumentException("Invalid lesson id.", nameof(lessonId));
            return new LessonResult { StudentId = studentId, LessonId = lessonId };
        }
        /// <summary>
        /// המסלול הרגיל: הציון הסופי הוא הציון שהמערכת חישבה.
        /// <para>
        /// ⚠️ <paramref name="computedScore"/> חייב להגיע מ-<c>LessonScoreCalculator</c>
        /// ולא מגוף הבקשה. עד לתיקון הזה הפרמטר היה בדיוק מה שהמורה הקלידה בדפדפן, והמקום
        /// היחיד שבו הציון הסופי נגזר היה המסך.
        /// </para>
        /// </summary>
        /// <param name="maxScore">
        /// תקרת השיעור — <c>100 + Σ BonusValue</c>, מ-<c>LessonScoreCalculator</c>.
        /// ⚠️ לא דגל בונוס ולא 150: התקרה השטוחה ההיא לא נגזרה משום דבר, וגודל הבונוס
        /// שהמורה הזינה לא השפיע עליה.
        /// </param>
        public void CompleteWith(double computedScore, double maxScore = Assignment.TotalPoints)
        {
            GuardCanComplete(computedScore, maxScore);

            FinalScore = computedScore;
            ComputedScore = computedScore;

            // סיכום חוזר אחרי פתיחה מחדש חייב לנקות דריסה קודמת, אחרת ציון שחזר להיות
            // מחושב היה ממשיך להיראות כמו ציון שנקבע ידנית.
            FinalScoreOverriddenByUserId = null;
            FinalScoreOverriddenAt = null;
            FinalScoreOverrideReason = null;

            IsComplete = true;
            CalculatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// המורה קובעת ציון סופי אחר מזה שחושב — רשת ביטחון, לא המסלול הרגיל.
        /// <para>
        /// קיים כי לא כל מה שקובע ציון נמצא במערכת: <c>CompleteLessonHandler</c> מתיר
        /// בכוונה לסכם שיעור שבו ההגשה במצב <c>AiFailed</c>, כדי שיהיה אפשר לתת ציון ידני
        /// כשהבדיקה האוטומטית נכשלה. בלי מסלול כזה שיעור כזה היה נשאר בלי ציון לנצח.
        /// </para>
        /// <param name="computedScore">
        /// מה שהמערכת חישבה — נשמר לצד הציון שנקבע. <c>null</c> כשאף תרגיל לא נבדק.
        /// </param>
        /// </summary>
        public void CompleteWithOverride(
            double? computedScore,
            double overrideScore,
            int teacherUserId,
            string reason,
            double maxScore = Assignment.TotalPoints)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("A reason is required — it is the audit trail", nameof(reason));

            GuardCanComplete(overrideScore, maxScore);

            FinalScore = overrideScore;
            ComputedScore = computedScore;

            FinalScoreOverriddenByUserId = teacherUserId;
            FinalScoreOverriddenAt = DateTime.UtcNow;
            FinalScoreOverrideReason = reason;

            IsComplete = true;
            CalculatedAt = DateTime.UtcNow;
        }

        private void GuardCanComplete(double score, double maxScore)
        {
            if (IsComplete) throw new InvalidOperationException("Already completed.");

            // התקרה מגיעה מהמחשבון ולא מקבוע: היא 100 בשיעור בלי בונוס, ו-100 ועוד סכום
            // ה-BonusValue של תרגילי הבונוס שבו. הסובלנות היא כדי שהתקרה עצמה תתקבל אחרי
            // עיגול לספרה אחת, בדיוק כמו שהציון המחושב מתקבל.
            if (score < 0 || score > maxScore + 0.05)
                throw new ArgumentOutOfRangeException(
                    nameof(score),
                    $"Score must be between 0 and {maxScore}.");
        }

        /// <summary>
        /// פותחת מחדש ציון סופי שכבר נסגר.
        /// <para>
        /// ⚠️ <c>CompleteWith</c> זורק "Already completed", ולכן <b>ציון סופי שגוי לא היה
        /// ניתן לתיקון בשום דרך</b> — לא דרך ה-API ולא דרך המסך. זו רשת הביטחון היחידה
        /// לטעות של מורה, והיא גם מה שמשחרר את ההגשות של אותה תלמידה בשיעור להגשה חוזרת.
        /// </para>
        /// </summary>
        public void Reopen()
        {
            if (!IsComplete)
                throw new InvalidOperationException("Lesson result is not completed.");

            IsComplete = false;
            CalculatedAt = DateTime.UtcNow;

            // הציון עצמו נשאר: הוא ההצעה שממנה המורה תתקן, ומחיקתו הייתה מאלצת אותה
            // לחשב הכול מחדש רק כדי לשנות ספרה אחת.
        }

        public Student Student { get; set; } = null!;
        public Lesson Lesson { get; set; } = null!;

    }
}


