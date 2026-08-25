using SmartGrader.Domain.Entities;

namespace SmartGrader.UnitTests.Helpers
{
    /// <summary>
    /// ישויות לצורכי בדיקה. כולן בעלות בנאי מוגן (נוצרות דרך EF או דרך factory), ותת-מחלקה
    /// היא הדרך החוקית להגיע אליו — בלי לרופף שום access modifier בקוד הייצור.
    /// <para>
    /// ⚠️ <c>Id</c> הוא היוצא מן הכלל: ה-setter שלו פרטי (מפתח שנוצר ב-DB, גם EF קובע אותו
    /// ברפלקציה), ולכן הוא נקבע כאן ברפלקציה נקודתית. ר' <see cref="TestAssignment"/>.
    /// </para>
    /// </summary>
    public static class TestEntities
    {
        private static T WithId<T>(T entity, int id)
        {
            typeof(T).GetProperty("Id")!.SetValue(entity, id);
            return entity;
        }

        public sealed class TestLesson : Lesson
        {
            public TestLesson(int id, int teacherId, params SchoolClass[] classes)
            {
                WithId<Lesson>(this, id);
                TeacherId = teacherId;
                Subject = "מבוא למדעי המחשב";
                Classes = classes.ToList();
            }
        }

        public sealed class TestStudent : Student
        {
            public TestStudent(int id, int classId, SchoolClass? schoolClass = null)
            {
                WithId<Student>(this, id);
                ClassId = classId;
                FullName = "תלמידה לבדיקה";
                Class = schoolClass!;
            }
        }

        public static SchoolClass Class(int id, bool isArchived = false)
        {
            var schoolClass = SchoolClass.Create("י\"א 3", academicYear: 5786);
            schoolClass.IsArchived = isArchived;
            return WithId(schoolClass, id);
        }
    }
}
