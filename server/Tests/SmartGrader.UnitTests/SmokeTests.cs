using FluentAssertions;
using Xunit;

namespace SmartGrader.UnitTests
{
    /// <summary>
    /// בדיקת עשן אחת שמוכיחה שצינור הבדיקות עובד — פרויקט, חבילות, גילוי והרצה.
    /// נשארת בכוונה: אם היא אדומה, הבעיה בצינור ולא בקוד.
    /// </summary>
    public class SmokeTests
    {
        // הצינור רץ — קומפילציה, גילוי טסטים, והרצה
        [Fact]
        public void TestPipeline_Runs()
        {
            true.Should().BeTrue();
        }
    }
}
