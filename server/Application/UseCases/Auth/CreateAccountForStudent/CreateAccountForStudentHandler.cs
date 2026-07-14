using AutoMapper;
using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Common.Interfaces;
using SmartGrader.Application.Dtos.Student;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Auth.CreateAccountForStudent
{
    public class CreateAccountForStudentHandler
        : IRequestHandler<CreateAccountForStudentCommand, StudentResponseDto>
    {
        private readonly IStudentRepository _studentRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasherService _passwordHasher;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CreateAccountForStudentHandler(
            IStudentRepository studentRepository,
            IUserRepository userRepository,
            IPasswordHasherService passwordHasher,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _studentRepository = studentRepository;
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<StudentResponseDto> Handle(
            CreateAccountForStudentCommand request,
            CancellationToken cancellationToken)
        {
            var student = await _studentRepository.GetByIdAsync(request.StudentId, cancellationToken);

            if (student is null)
                throw new NotFoundException(nameof(Student), request.StudentId);

            if (student.UserId is not null)
                throw new BusinessRuleException("Student already has a login account.");

            if (await _userRepository.ExistsByEmailAsync(request.Dto.Email, cancellationToken))
                throw new UniqueConstraintException("A user with this email already exists.");

            var user = User.Create(
                request.Dto.Email,
                _passwordHasher.Hash(request.Dto.Password),
                student.FullName,
                UserRole.Student);

            student.User = user;
            await _studentRepository.UpdateAsync(student, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<StudentResponseDto>(student);
        }
    }
}
