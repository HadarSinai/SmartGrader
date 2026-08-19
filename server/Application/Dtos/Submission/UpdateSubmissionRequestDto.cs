namespace SmartGrader.Application.Dtos.Submissions
{
    public class UpdateSubmissionRequestDto
    {
        public string SourceCode { get; set; } = string.Empty;

        // בלי Files תלמידה בתרגיל רב-קובצי נשארת תקועה גם אחרי שהעריכה נפתחה לה: SourceCode
        // ריק בהגשות האלה, וההגשה החוזרת הייתה מוחקת בפועל את הקוד. null = הגשה חד-קובצית.
        public List<SubmissionFileDto>? Files { get; set; }
    }
}
