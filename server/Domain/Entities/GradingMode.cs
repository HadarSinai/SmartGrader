namespace SmartGrader.Domain.Entities
{
    // אופן הבדיקה של תרגיל — נקבע ע"י המורה בטופס התרגיל וקובע איך ה-Runner מריץ את הקוד
    // ואילו הנחיות התלמיד רואה במסך ההגשה.
    public enum GradingMode
    {
        // תוכנית שלמה עם Main של התלמיד — רצה כמו שהיא (בלי עטיפה), קלט הטסט הוא stdin מלא.
        FullProgram,

        // מתודה בודדת — הקוד נעטף ב-StudentSolution ונקרא לפי MethodName, קלט מופרד ברווחים.
        Method,

        // פרויקט רב-קובצי הנבדק דרך מתודת כניסה מ-ExpectedFiles, קלט כמערך JSON.
        MultiFileMethod
    }
}
