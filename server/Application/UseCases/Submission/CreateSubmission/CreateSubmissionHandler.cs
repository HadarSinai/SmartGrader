using AutoMapper;
using FluentValidation.Results;
using Hangfire;
using MediatR;
using SmartGrader.Application.Common.Authorization;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Dtos.Submissions;
using SmartGrader.Application.Services.BackgroundJobs;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Submissions.CreateSubmission
{
    public class CreateSubmissionHandler
        : IRequestHandler<CreateSubmissionCommand, SubmissionResponseDto>
    {
        private readonly ISubmissionRepository _submissionRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly ILessonRepository _lessonRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IBackgroundJobClient _jobClient;

        public CreateSubmissionHandler(
            ISubmissionRepository submissionRepository,
            IStudentRepository studentRepository,
            IAssignmentRepository assignmentRepository,
            ILessonRepository lessonRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IBackgroundJobClient jobClient)
        {
            _submissionRepository = submissionRepository;
            _studentRepository = studentRepository;
            _assignmentRepository = assignmentRepository;
            _lessonRepository = lessonRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _jobClient = jobClient;
        }

        public async Task<SubmissionResponseDto> Handle(
            CreateSubmissionCommand request,
            CancellationToken cancellationToken)
        {
            var dto = request.Dto;

            // ✔ בדיקה שהתלמיד קיים
            var student = await _studentRepository
                .GetByIdAsync(request.StudentId, cancellationToken);

            if (student is null)
                throw new NotFoundException(nameof(Student), request.StudentId);

            // ✔ בדיקה שהמשימה קיימת
            var assignment = await _assignmentRepository
                .GetByIdAsync(dto.AssignmentId, cancellationToken);

            if (assignment is null)
                throw new NotFoundException(nameof(Assignment), dto.AssignmentId);

            // ✔ בדיקה שהשיעור של התרגיל אכן משויך לכיתה של התלמידה. בלי זה "קיים תלמיד + קיים
            // תרגיל" הספיק כדי להגיש לכל תרגיל במערכת. NotFound על התרגיל (ולא Forbid) כדי לא
            // לאשר שהתרגיל קיים בכלל.
            var lesson = await _lessonRepository.GetByIdAsync(assignment.LessonId, cancellationToken);

            if (lesson is null || !LessonAccess.IsAssignedToClass(lesson, student.ClassId))
                throw new NotFoundException(nameof(Assignment), dto.AssignmentId);

            // ✔ הגשה אחת בלבד לכל (StudentId, AssignmentId). בלי הבדיקה הזו תלמידה שלא אהבה
            // את הציון פשוט הגישה שוב וקיבלה שורה מנוקדת שנייה, ושתיהן נספרו בממוצע. הכלל נאכף
            // גם באינדקס ייחודי ב-DB, כך ששתי לחיצות במקביל לא יוצרות שתי שורות.
            var existing = await _submissionRepository
                .GetByStudentAndAssignmentAsync(request.StudentId, dto.AssignmentId, cancellationToken);

            if (existing is not null)
                throw new BusinessRuleException(
                    $"כבר קיימת הגשה לתרגיל הזה (הגשה #{existing.Id}). לא ניתן להגיש פעם נוספת.");

            // ✔ הגשה רב-קובצית: כאשר לתרגיל יש ExpectedFiles מוגדרים, מחייבים שהקבצים שהוגשו
            // יתאימו (לפי FileName) לרשימת הקבצים הצפויה של התרגיל.
            if (assignment.ExpectedFiles.Count > 0)
            {
                var submittedNames = (dto.Files ?? new())
                    .Select(f => f.FileName)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var missing = assignment.ExpectedFiles
                    .Select(f => f.FileName)
                    .Where(name => !submittedNames.Contains(name))
                    .ToList();

                if (missing.Count > 0)
                {
                    throw new AppValidationException(new[]
                    {
                        new ValidationFailure(
                            nameof(dto.Files),
                            $"Missing required file(s): {string.Join(", ", missing)}")
                    });
                }
            }

            var sourceFiles = (dto.Files ?? new())
                .Select(f => new SubmissionFile { FileName = f.FileName, Content = f.Content })
                .ToList();

            // ✔ יצירת Submission דרך ctor בלבד (PendingAi)
            var submission = new Submission(
                request.StudentId,
                dto.AssignmentId,
                dto.SourceCode,
                sourceFiles
            );

            // ✔ שמירה ב־DB (בלי AI, בלי ציונים)
            await _submissionRepository.AddAsync(submission, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _jobClient.Enqueue<IGradeSubmissionJob>(job => job.ExecuteAsync(submission.Id));

            // ✔ החזרה ללקוח
            return _mapper.Map<SubmissionResponseDto>(submission);
        }
    }
}
