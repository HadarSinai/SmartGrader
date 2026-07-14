using AutoMapper;
using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Common.Interfaces;
using SmartGrader.Application.Dtos.Student;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Auth.CreateStudentAccount
{
    public class CreateStudentAccountHandler
        : IRequestHandler<CreateStudentAccountCommand, StudentResponseDto>
    {
        private readonly IUserRepository _userRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IPasswordHasherService _passwordHasher;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CreateStudentAccountHandler(
            IUserRepository userRepository,
            IStudentRepository studentRepository,
            IPasswordHasherService passwordHasher,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _userRepository = userRepository;
            _studentRepository = studentRepository;
            _passwordHasher = passwordHasher;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<StudentResponseDto> Handle(
            CreateStudentAccountCommand request,
            CancellationToken cancellationToken)
        {
            if (await _userRepository.ExistsByEmailAsync(request.Dto.Email, cancellationToken))
                throw new UniqueConstraintException("A user with this email already exists.");

            var user = User.Create(
                request.Dto.Email,
                _passwordHasher.Hash(request.Dto.Password),
                request.Dto.FullName,
                UserRole.Student);

            var student = _mapper.Map<Student>(request.Dto);
            student.User = user;

            await _userRepository.AddAsync(user, cancellationToken);
            await _studentRepository.AddAsync(student, cancellationToken);

            // Single SaveChanges = single transaction (User + Student created atomically)
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<StudentResponseDto>(student);
        }
    }
}
