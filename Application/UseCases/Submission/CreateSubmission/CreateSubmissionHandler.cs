using AutoMapper;
using Domain.Abstractions;
using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Common.Services;
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
     //   private readonly ICompilerService _compilerService;
        private readonly IMapper _mapper;

        public CreateSubmissionHandler(
            ISubmissionRepository repository,
            IStudentRepository studentRepository,
            IAssignmentRepository assignmentRepository,
            IUnitOfWork unitOfWork,
        //    ICompilerService compilerService,
            IMapper mapper)
        {
            _repository = repository;
            _studentRepository = studentRepository;
            _assignmentRepository = assignmentRepository;
            _unitOfWork = unitOfWork;
         //   _compilerService = compilerService;
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

            // ✔ יצירת ישות Submission (לפי המבנה שלך – תתאימי אם הקונסטרקטור מעט שונה)
            var submission = new Submission
            {
                StudentId = request.StudentId,
                AssignmentId = dto.AssignmentId,
                SourceCode = dto.SourceCode,
                Score = 0,
                Comments = string.Empty
            };

            // ✔ קומפילציה של הקוד
        //    var compileResult = await _compilerService.CompileAsync(dto.SourceCode, cancellationToken);

           // if (compileResult.IsSuccess)
            //{
            //    // הצלחה – ציון מלא והערה חיובית
            //    submission.MarkCheckedByAi(
            //        score: 100,
            //        comments: "Compilation succeeded");
            //}
            //else
            //{
            //    var errorsText = string.Join(Environment.NewLine, compileResult.Errors);

            //    submission.MarkCheckedByAi(
            //        score: 0,
            //        comments: errorsText);

            //}

            await _repository.AddAsync(submission, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<SubmissionResponseDto>(submission);
        }
    }
}
