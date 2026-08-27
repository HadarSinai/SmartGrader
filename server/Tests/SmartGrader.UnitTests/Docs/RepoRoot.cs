namespace SmartGrader.UnitTests.Docs
{
    /// <summary>
    /// איתור שורש הריפו מתוך תיקיית ההרצה, ונתיבים לקבצים שמבחני ה-conformance קוראים.
    /// <para>
    /// ⚠️ לא ספירת "../" — היא נשברת כשמשנים TargetFramework או Configuration, והשבירה
    /// נראית בדיוק כמו מסמך ריק, כלומר כמו טסט ירוק.
    /// </para>
    /// </summary>
    internal static class RepoRoot
    {
        public static string Path { get; } = Locate();

        private static string Locate()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);

            while (dir is not null)
            {
                // שתי התיקיות יחד מזהות את השורש חד-משמעית
                if (Directory.Exists(System.IO.Path.Combine(dir.FullName, "docs")) &&
                    Directory.Exists(System.IO.Path.Combine(dir.FullName, "server")))
                {
                    return dir.FullName;
                }

                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException(
                $"שורש הריפו לא נמצא מתוך {AppContext.BaseDirectory}");
        }

        public static string DocsDir => System.IO.Path.Combine(Path, "docs");

        public static string Doc(string relative) =>
            System.IO.Path.Combine(DocsDir, relative);

        public static string ReadDoc(string relative) => File.ReadAllText(Doc(relative));

        public static string ControllersDir =>
            System.IO.Path.Combine(Path, "server", "Api", "Controllers");

        public static string ClientRoutesFile =>
            System.IO.Path.Combine(Path, "client", "src", "app", "app.routes.ts");

        /// <summary>
        /// המסמכים של המפרט החדש. הסט הישן תחת <c>docs/ux/</c> ו-<c>auth-plan.md</c> אינו
        /// נכלל: הוא נמחק בשלב A7, והקישורים שלו כבר שבורים — <c>assignments-jtbd.md</c>
        /// מפנה אל <c>assignment-extended.model.ts</c> שנמחק. להחיל עליו את התקן שהוא עצמו
        /// נכשל בו היה חוסם את הריפו על מסמך שנמצא בדרך למחיקה.
        /// </summary>
        public static IReadOnlyList<string> SpecDocs()
        {
            var uxDir = System.IO.Path.Combine(DocsDir, "ux") + System.IO.Path.DirectorySeparatorChar;

            return Directory
                .GetFiles(DocsDir, "*.md", SearchOption.AllDirectories)
                .Where(f => !f.StartsWith(uxDir, StringComparison.OrdinalIgnoreCase))
                .Where(f => !System.IO.Path.GetFileName(f)
                    .Equals("auth-plan.md", StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f, StringComparer.Ordinal)
                .ToList();
        }
    }
}
