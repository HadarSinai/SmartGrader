using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Common.Interfaces;
using SmartGrader.Application.Dtos.Auth;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Auth.RegisterTeacher
{
    public class RegisterTeacherHandler : IRequestHandler<RegisterTeacherCommand, AuthResponseDto>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasherService _passwordHasher;
        private readonly IJwtTokenGenerator _tokenGenerator;
        private readonly IUnitOfWork _unitOfWork;

        public RegisterTeacherHandler(
            IUserRepository userRepository,
            IPasswordHasherService passwordHasher,
            IJwtTokenGenerator tokenGenerator,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _tokenGenerator = tokenGenerator;
            _unitOfWork = unitOfWork;
        }

        public async Task<AuthResponseDto> Handle(RegisterTeacherCommand request, CancellationToken cancellationToken)
        {
            if (await _userRepository.ExistsByEmailAsync(request.Dto.Email, cancellationToken))
                throw new UniqueConstraintException("A user with this email already exists.");

            var user = User.Create(
                request.Dto.Email,
                _passwordHasher.Hash(request.Dto.Password),
                request.Dto.FullName,
                UserRole.Teacher);

            await _userRepository.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var token = _tokenGenerator.GenerateToken(user, studentId: null);

            return new AuthResponseDto(token, user.FullName, user.Role.ToString(), null);
        }
    }
}
