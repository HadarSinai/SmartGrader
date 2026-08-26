using AutoMapper;
using MediatR;
using SmartGrader.Application.Common.Authorization;
using SmartGrader.Application.Dtos.Submissions;
using SmartGrader.Domain.Abstractions;

namespace SmartGrader.Application.UseCases.Submissions.GetSubmissions
{
    public class GetSubmissionsHandler
        : IRequestHandler<GetSubmissionsQuery, IReadOnlyList<SubmissionResponseDto>>
    {
        private readonly ISubmissionRepository _repository;
        private readonly ILessonResultRepository _lessonResults;
        private readonly IMapper _mapper;

        public GetSubmissionsHandler(
            ISubmissionRepository repository,
            ILessonResultRepository lessonResults,
            IMapper mapper)
        {
            _repository = repository;
            _lessonResults = lessonResults;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<SubmissionResponseDto>> Handle(
            GetSubmissionsQuery request,
            CancellationToken cancellationToken)
        {
            var submissions = await _repository.GetByStudentIdAsync(
                request.StudentId,
                request.TeacherId,
                cancellationToken);

            if (submissions.Count == 0)
                return Array.Empty<SubmissionResponseDto>();

            // ⚠️ רשימת ההגשות נושאת TestResults מלאים בדיוק כמו הפריט הבודד — אותו סינון.
            var dtos = TestVisibility.RedactTestResults(
                _mapper.Map<IReadOnlyList<SubmissionResponseDto>>(submissions),
                request.IsStudentCaller);

            // ...ובדיוק כמו הפריט הבודד, גם CanResubmit כאן מכיר רק את סף הציון. שאילתה אחת
            // לכל הרשימה — כל ההגשות שייכות לתלמידה אחת.
            return await SubmissionLock.ApplyAsync(_lessonResults, dtos, submissions, cancellationToken);
        }
    }
}
