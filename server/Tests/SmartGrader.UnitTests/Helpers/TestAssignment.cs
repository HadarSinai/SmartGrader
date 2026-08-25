using SmartGrader.Domain.Entities;

namespace SmartGrader.UnitTests.Helpers
{
    /// <summary>
    /// תרגיל לצורכי בדיקה. ל-<see cref="Assignment"/> יש בנאי מוגן בלבד (ישויות נוצרות
    /// דרך EF), ותת-מחלקה היא הדרך החוקית להגיע אליו — בלי לרופף שום access modifier
    /// בקוד הייצור.
    /// </summary>
    /// <remarks>
    /// ⚠️ <c>Id</c> הוא היוצא מן הכלל היחיד: ה-setter שלו פרטי (מפתח שנוצר ב-DB, גם EF
    /// קובע אותו ברפלקציה), ולכן הוא נקבע כאן ברפלקציה נקודתית. מצב דומייני
    /// (ציון, סטטוס, בונוס) לעולם לא נקבע כך — רק דרך ה-API האמיתי.
    /// </remarks>
    public sealed class TestAssignment : Assignment
    {
        public TestAssignment(int id, bool isBonus = false, int testsAllocation = TotalPoints)
        {
            typeof(Assignment).GetProperty(nameof(Id))!.SetValue(this, id);
            IsBonus = isBonus;
            TestsAllocation = testsAllocation;
        }
    }
}
