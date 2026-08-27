using System.Text.RegularExpressions;

namespace SmartGrader.UnitTests.Docs
{
    /// <summary>
    /// בלוק מסומן במסמך: <c>&lt;!-- gen:kind argument --&gt;</c> … <c>&lt;!-- /gen --&gt;</c>,
    /// והשורות של טבלאות ה-Markdown שבתוכו.
    /// <para>
    /// ההערה בלתי נראית בכל מציג Markdown, ולכן המסמך נקרא כפרוזה לבעלת המערכת וכבלוק
    /// מכונה לטסט.
    /// </para>
    /// </summary>
    internal sealed record GenBlock(string Kind, string Argument, IReadOnlyList<string[]> Rows)
    {
        private static readonly Regex OpenMarker =
            new(@"^<!--\s*gen:(?<kind>[A-Za-z]+)(?:\s+(?<arg>[^>]*?))?\s*-->\s*$", RegexOptions.Compiled);

        private static readonly Regex CloseMarker =
            new(@"^<!--\s*/gen\s*-->\s*$", RegexOptions.Compiled);

        private static readonly Regex SeparatorCell =
            new(@"^:?-{1,}:?$", RegexOptions.Compiled);

        /// <summary>
        /// כל הבלוקים מסוג <paramref name="kind"/> במסמך. פרסר מכוון-טיפשות: כל תחכום כאן
        /// הוא באג שמייצר ירוק שקרי.
        /// </summary>
        public static IReadOnlyList<GenBlock> FindAll(string markdown, string kind)
        {
            var blocks = new List<GenBlock>();
            var lines = markdown.Replace("\r\n", "\n").Split('\n');

            for (var i = 0; i < lines.Length; i++)
            {
                var open = OpenMarker.Match(lines[i].Trim());
                if (!open.Success || !open.Groups["kind"].Value.Equals(kind, StringComparison.Ordinal))
                    continue;

                var tableLines = new List<string>();
                var j = i + 1;

                for (; j < lines.Length && !CloseMarker.IsMatch(lines[j].Trim()); j++)
                {
                    var line = lines[j].Trim();
                    if (line.StartsWith('|'))
                        tableLines.Add(line);
                }

                if (j == lines.Length)
                    throw new InvalidOperationException(
                        $"בלוק gen:{kind} נפתח ולא נסגר ב-<!-- /gen -->");

                blocks.Add(new GenBlock(
                    open.Groups["kind"].Value,
                    open.Groups["arg"].Value.Trim(),
                    DataRows(tableLines)));

                i = j;
            }

            return blocks;
        }

        /// <summary>
        /// משאיר רק שורות נתונים: מסיר את שורות ההפרדה, ואת שורת הכותרת שלפני כל אחת מהן.
        /// כך אפשר לשים כמה טבלאות בבלוק אחד — מטריצת ההרשאות מפוצלת לפי controller.
        /// </summary>
        private static List<string[]> DataRows(List<string> tableLines)
        {
            var skip = new HashSet<int>();

            for (var i = 0; i < tableLines.Count; i++)
            {
                if (!IsSeparator(tableLines[i]))
                    continue;

                skip.Add(i);
                if (i > 0) skip.Add(i - 1); // שורת הכותרת
            }

            return tableLines
                .Where((_, i) => !skip.Contains(i))
                .Select(Cells)
                .ToList();
        }

        private static bool IsSeparator(string line)
        {
            var cells = Cells(line);
            return cells.Length > 0 && cells.All(c => SeparatorCell.IsMatch(c));
        }

        /// <summary>תאי שורה, בלי ה-pipe בקצוות ובלי גרשי הקוד.</summary>
        private static string[] Cells(string line) =>
            line.Trim('|')
                .Split('|')
                .Select(c => c.Trim().Trim('`').Trim())
                .ToArray();
    }
}
