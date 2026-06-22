using AutoMapper;
using Domain.Abstractions;
using MediatR;
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
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IAiJobQueue _aiQueue;

        public CreateSubmissionHandler(
            ISubmissionRepository submissionRepository,
            IStudentRepository studentRepository,
            IAssignmentRepository assignmentRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper, IAiJobQueue aiQueue)
        {
            _submissionRepository = submissionRepository;
            _studentRepository = studentRepository;
            _assignmentRepository = assignmentRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _aiQueue = aiQueue;

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

            // ✔ יצירת Submission דרך ctor בלבד (PendingAi)
            var submission = new Submission(
                request.StudentId,
                dto.AssignmentId,
                dto.SourceCode
            );

            // ✔ שמירה ב־DB (בלי AI, בלי ציונים)
            await _submissionRepository.AddAsync(submission, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _aiQueue.EnqueueAsync(submission.Id, cancellationToken);

            // ✔ החזרה ללקוח
            return _mapper.Map<SubmissionResponseDto>(submission);
        }
    }
}
