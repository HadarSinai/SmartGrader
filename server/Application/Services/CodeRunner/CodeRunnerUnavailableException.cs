namespace SmartGrader.Application.Services.CodeRunner;

/// <summary>
/// כשל תשתית של מערכת הרצת הקוד (Judge0 החזיר Internal Error) — להבדיל מכשל
/// קומפילציה של קוד התלמיד או כשל של ה-AI. מנותב ב-AiWorker ל-MarkJudgeUnavailable.
/// </summary>
public class CodeRunnerUnavailableException : Exception
{
    public CodeRunnerUnavailableException(string message) : base(message) { }
}
