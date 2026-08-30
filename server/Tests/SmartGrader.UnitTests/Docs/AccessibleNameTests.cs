using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace SmartGrader.UnitTests.Docs
{
    /// <summary>
    /// <c>D-5</c> ו-<c>D-6</c> מ-<c>docs/design-system.md</c>: לכפתור אייקון יש שם, ולשדה
    /// טופס יש שם. שני אלה הם החלק היחיד מ-<c>D-1…D-15</c> שאפשר לקבוע ממקור בלבד — השאר
    /// דורש מקלדת, קורא מסך או מד ניגודיות, והמסמך אומר זאת במפורש.
    /// <para>
    /// ⚠️ פרסור טקסט על תבניות HTML, מאותה סיבה כמו <see cref="DesignTokenTests"/>: אין
    /// ללקוח פרויקט טסטים, ולכן טסט בשרת שקורא את הקבצים הוא השומר היחיד שיש.
    /// </para>
    /// <para>
    /// ⚠️ הבדיקה הזו נכתבה אחרי שטופס התרגילים נשבר בלי שאף טסט הרגיש. אבל מה שהיא מצאה
    /// כשהורצה לראשונה לא היה מסך אחד: <b>39 רכיבי PrimeNG בכל המערכת נשאו שם שקורא מסך
    /// לעולם אינו מקריא.</b> ר' <see cref="PrimeNgComponents_DoNotCarryANameThatNeverReaches"/>.
    /// </para>
    /// </summary>
    public class AccessibleNameTests
    {
        /// <summary>
        /// אפס בשלושת המונים. <b>לעולם לא להעלות את המספרים האלה.</b> שדה בלי שם אינו
        /// חוב טכני שמשלמים בהמשך — הוא מסך שמישהי לא יכולה למלא.
        /// </summary>
        private const int Ratchet = 0;

        /// <summary>
        /// רכיבים שמקבלים קלט מהמשתמשת. <c>p-tableCheckbox</c> ו-<c>p-tableHeaderCheckbox</c>
        /// נכללים: הם תיבות סימון לכל דבר, והשם שלהן הוא מה שמבדיל בין "בחירת שורה" לבין
        /// "בחירת הכול".
        /// </summary>
        private static readonly HashSet<string> Controls = new(StringComparer.OrdinalIgnoreCase)
        {
            "input", "textarea", "select",
            "p-dropdown", "p-inputNumber", "p-checkbox", "p-calendar", "p-multiSelect",
            "p-password", "p-selectButton", "p-inputSwitch", "p-radioButton",
            "p-autoComplete", "p-inputMask", "p-chips", "p-slider", "p-rating",
            "p-triStateCheckbox", "p-listbox", "p-treeSelect", "p-colorPicker",
            "p-toggleButton", "p-fileUpload", "p-editor", "p-cascadeSelect", "p-knob",
            "p-tableCheckbox", "p-tableHeaderCheckbox",
        };

        // <tag ...> על פני שורות, בלי לבלוע > שנמצא בתוך מחרוזת של binding
        private static readonly Regex Tag =
            new(@"<(?<tag>[a-zA-Z][a-zA-Z0-9-]*)(?<attrs>(?:""[^""]*""|'[^']*'|[^>""'])*)>",
                RegexOptions.Compiled);

        // על אלמנט HTML אמיתי, id ו-aria-* עובדים כפי שהם נכתבים
        private static readonly Regex NativeName =
            new(@"(?:^|\s)(?:\[?attr\.aria-label(?:ledby)?\]?|aria-label(?:ledby)?|\[?id\]?)\s*=",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// ⚠️ על רכיב PrimeNG רק ה-<c>@Input</c> נספר, ובאותיות המדויקות שלו. ר' ההסבר
        /// ב-<see cref="PrimeNgComponents_DoNotCarryANameThatNeverReaches"/>.
        /// </summary>
        private static readonly Regex PrimeName =
            new(@"(?:^|\s)\[?(?:inputId|ariaLabel|ariaLabelledBy)\]?\s*=", RegexOptions.Compiled);

        private static readonly Regex RawAria =
            new(@"(?:^|\s)(?:\[?attr\.aria-label(?:ledby)?\]?|aria-label(?:ledby)?)\s*=",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex HasIcon =
            new(@"(?:^|\s)\[?icon\]?\s*=", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex HasLabel =
            new(@"(?:^|\s)\[?label\]?\s*=", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// היוצא מן הכלל היחיד, והוא מתועד: <c>submit-code</c> ו-<c>students-list</c> מחזיקים
        /// כל אחד <c>&lt;input type="file" class="hidden"&gt;</c> שנפתח מכפתור בעל שם. הוא אינו
        /// פקד שהמשתמשת מגיעה אליו, ומתן שם לו אינו מוסיף דבר.
        /// </summary>
        private static readonly Regex HiddenFileInput =
            new(@"class\s*=\s*""[^""]*\bhidden\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // D-6 — אף שדה טופס אינו נשען על placeholder כשם היחיד שלו
        [Fact]
        public void EveryFormControl_HasAnAccessibleName()
        {
            var offenders = Scan((tag, attrs) =>
                Controls.Contains(tag)
                && !(tag.Equals("input", StringComparison.OrdinalIgnoreCase) && HiddenFileInput.IsMatch(attrs))
                && !IsNamed(tag, attrs));

            offenders.Should().HaveCountLessThanOrEqualTo(Ratchet,
                "שדה בלי id/inputId/aria-label נקרא לקורא מסך כ״עריכה, ריק״. תווית שקושרה " +
                "ל-for, או ariaLabel כשאין תווית גלויה. כרגע: " + Join(offenders));
        }

        // D-5 — כפתור שכל תוכנו אייקון מכריז על הפעולה *ועל הנושא שלה*
        [Fact]
        public void EveryIconOnlyButton_HasAnAccessibleName()
        {
            var offenders = Scan((tag, attrs) =>
                (tag.Equals("p-button", StringComparison.OrdinalIgnoreCase)
                 || tag.Equals("button", StringComparison.OrdinalIgnoreCase))
                && HasIcon.IsMatch(attrs)
                && !HasLabel.IsMatch(attrs)
                && !IsNamed(tag, attrs));

            offenders.Should().HaveCountLessThanOrEqualTo(Ratchet,
                "כפתור אייקון בלי שם מוכרז כ״לחצן״ — בלי פעולה ובלי נושא. D-5 דורש את שניהם, " +
                "למשל ariaLabel=\"מחיקת מקרה בדיקה 2\". כרגע: " + Join(offenders));
        }

        /// <summary>
        /// ⚠️ הבדיקה שתפסה את מה שהשתיים שמעליה החמיצו. רכיב PrimeNG מרנדר
        /// <c>&lt;button&gt;</c> או <c>&lt;input&gt;</c> פנימי, ומעביר אליו רק את ה-<c>@Input</c>
        /// שלו (<c>[attr.aria-label]="ariaLabel"</c> בתבנית של הרכיב). תכונה גולמית שנכתבת על
        /// האלמנט <c>&lt;p-button&gt;</c> עצמו נשארת על עטיפה חסרת role — ו-<c>aria-label</c>
        /// על אלמנט חסר role מתעלמים ממנו לחלוטין.
        /// <para>
        /// כלומר <c>[attr.aria-label]="'מחיקת שיעור: ' + name"</c> נראה נכון בקוד, נראה נכון
        /// ב-DOM, ואינו מושמע. <b>39 מופעים כאלה היו בקוד</b> — כמעט כל כפתור שורה וכל מסנן
        /// בכל מסך רשימה — בזמן שהמסמך הצהיר שהמסך היחיד שנכשל ב-D-5 הוא טופס התרגילים.
        /// </para>
        /// </summary>
        [Fact]
        public void PrimeNgComponents_DoNotCarryANameThatNeverReaches()
        {
            var offenders = Scan((tag, attrs) =>
                tag.StartsWith("p-", StringComparison.OrdinalIgnoreCase) && RawAria.IsMatch(attrs));

            offenders.Should().HaveCountLessThanOrEqualTo(Ratchet,
                "aria-label גולמי על רכיב PrimeNG נשאר על העטיפה ואינו מגיע לאלמנט הפנימי — " +
                "שם שנראה קיים ואינו מושמע. הקלט הוא ariaLabel/ariaLabelledBy/inputId. כרגע: " +
                Join(offenders));
        }

        /// <summary>
        /// ⚠️ הוכחה שהשן נושכת. בלי זה, רג'קס שבור היה מחזיר אפס עבירות — כלומר טסט ירוק
        /// שאינו בודק דבר, בדיוק כמו רַצֶ'ט הצבעים שקרא 0 בזמן ששמונה קבצים עברו מתחתיו.
        /// </summary>
        [Fact]
        public void TheScan_ActuallyReadsTheTemplates()
        {
            Templates().Should().HaveCountGreaterThan(30,
                "אם כמעט לא נמצאו תבניות, איתור הקבצים נשבר ולא הקוד");

            // כל שלוש הבדיקות מזהות שדה שנשען על placeholder בלבד
            var probe = "<input pInputText formControlName=\"x\" placeholder=\"שם\" />";
            Tag.Matches(probe).Should().ContainSingle();
            NativeName.IsMatch(Tag.Match(probe).Groups["attrs"].Value).Should().BeFalse();

            var named = "<p-button icon=\"pi pi-trash\" ariaLabel=\"מחיקה\"></p-button>";
            PrimeName.IsMatch(Tag.Match(named).Groups["attrs"].Value).Should().BeTrue();

            var dead = "<p-button icon=\"pi pi-trash\" [attr.aria-label]=\"x\"></p-button>";
            RawAria.IsMatch(Tag.Match(dead).Groups["attrs"].Value).Should().BeTrue();
            PrimeName.IsMatch(Tag.Match(dead).Groups["attrs"].Value).Should().BeFalse();
        }

        private static bool IsNamed(string tag, string attrs) =>
            tag.StartsWith("p-", StringComparison.OrdinalIgnoreCase)
                ? PrimeName.IsMatch(attrs)
                : NativeName.IsMatch(attrs);

        private static List<string> Scan(Func<string, string, bool> isOffender)
        {
            var found = new List<string>();

            foreach (var file in Templates())
            {
                var html = File.ReadAllText(file);

                foreach (Match m in Tag.Matches(html))
                {
                    var tag = m.Groups["tag"].Value;
                    var attrs = m.Groups["attrs"].Value;

                    if (!isOffender(tag, attrs)) continue;

                    var line = html.Take(m.Index).Count(c => c == '\n') + 1;
                    found.Add($"{Path.GetRelativePath(RepoRoot.Path, file)}:{line} <{tag}>");
                }
            }

            return found;
        }

        private static IReadOnlyList<string> Templates() =>
            Directory.GetFiles(
                Path.Combine(RepoRoot.Path, "client", "src", "app"),
                "*.html",
                SearchOption.AllDirectories);

        private static string Join(IEnumerable<string> offenders) =>
            string.Join("; ", offenders);
    }
}
