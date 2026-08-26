using AutoMapper;
using MediatR;
using SmartGrader.Application.Common.Authorization;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Dtos.Submissions;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Submissions.GetSubmissionById
{
    public class GetSubmissionByIdHandler
        : IRequestHandler<GetSubmissionByIdQuery, SubmissionResponseDto>
    {
        private readonly ISubmissionRepository _repository;
        private readonly ILessonResultRepository _lessonResults;
        private readonly IMapper _mapper;

        public GetSubmissionByIdHandler(
            ISubmissionRepository repository,
            ILessonResultRepository lessonResults,
            IMapper mapper)
        {
            _repository = repository;
            _lessonResults = lessonResults;
            _mapper = mapper;
        }

        public async Task<SubmissionResponseDto> Handle(
            GetSubmissionByIdQuery request,
            CancellationToken cancellationToken)
        {
            // שולפים לפי מזהה ההגשה — כבר מסונן לפי בעלות המורה על השיעור
            var submission = await _repository.GetByIdAsync(
                request.SubmissionId,
                request.TeacherId,
                cancellationToken);

            if (submission is null)
                throw new NotFoundException(nameof(Submission), request.SubmissionId);

            // בדיקה שההגשה שייכת לתלמידה הספציפית
            if (submission.StudentId != request.StudentId)
                throw new NotFoundException(
                    "Submission does not belong to this student.",
                    request.SubmissionId);

            // ⚠️ נתיב הדלף השני: אחרי הבדיקה TestResults נושא Input/Expected/Actual לכל מקרה,
            // כולל המוסתרים. מרוקנים כאן, ומשאירים רק Passed. ר' TestVisibility.
            var dto = TestVisibility.RedactTestResults(
                _mapper.Map<SubmissionResponseDto>(submission),
                request.IsStudentCaller);

            // ⚠️ זה ה-endpoint שמאחורי שני מסכי הפרטים, ו-CanResubmit שמגיע מהמיפוי מכיר רק
            // את סף הציון. בלי השורה הזו מסך התלמידה מציע "תיקון והגשה מחדש" על שיעור שסוכם.
            return await SubmissionLock.ApplyAsync(_lessonResults, dto, submission, cancellationToken);
        }
    }
}
