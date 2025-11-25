using AutoMapper;
using Domain.Abstractions;
using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Dtos.Submissions;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Submissions.CreateSubmission
{
    public class CreateSubmissionHandler
        : IRequestHandler<CreateSubmissionCommand, SubmissionResponseDto>
    {
        private readonly ISubmissionRepository _repository;
        private readonly IStudentRepository _studentRepository;
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CreateSubmissionHandler(
            ISubmissionRepository repository,
            IStudentRepository studentRepository,
            IAssignmentRepository assignmentRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _studentRepository = studentRepository;
            _assignmentRepository = assignmentRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<SubmissionResponseDto> Handle(
            CreateSubmissionCommand request,
            CancellationToken cancellationToken)
        {
            var dto = request.Dto;

            // ✔ בדיקה שהתלמיד קיים
            var student = await _studentRepository.GetByIdAsync(request.StudentId, cancellationToken);
            if (student is null)
                throw new NotFoundException(nameof(Student), request.StudentId);

            // ✔ בדיקה שהמשימה קיימת
            var assignment = await _assignmentRepository.GetByIdAsync(dto.AssignmentId, cancellationToken);
            if (assignment is null)
                throw new NotFoundException(nameof(Assignment), dto.AssignmentId);

            // ✔ מעבר DTO → Entity
            var submission = _mapper.Map<Submission>(dto);

            // ✔ שמירה ב־DB
            await _repository.AddAsync(submission, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // ✔ Entity → DTO
            return _mapper.Map<SubmissionResponseDto>(submission);
        }
    }
}
