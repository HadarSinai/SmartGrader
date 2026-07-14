using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Common.Interfaces;
using SmartGrader.Application.Dtos.Auth;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Auth.Login
{
    public class LoginHandler : IRequestHandler<LoginCommand, AuthResponseDto>
    {
        private const string InvalidCredentialsMessage = "Invalid email or password.";

        private readonly IUserRepository _userRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IPasswordHasherService _passwordHasher;
        private readonly IJwtTokenGenerator _tokenGenerator;

        public LoginHandler(
            IUserRepository userRepository,
            IStudentRepository studentRepository,
            IPasswordHasherService passwordHasher,
            IJwtTokenGenerator tokenGenerator)
        {
            _userRepository = userRepository;
            _studentRepository = studentRepository;
            _passwordHasher = passwordHasher;
            _tokenGenerator = tokenGenerator;
        }

        public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByEmailAsync(request.Dto.Email, cancellationToken);

            // Generic error — never reveal whether the email exists
            if (user is null || !_passwordHasher.Verify(user.PasswordHash, request.Dto.Password))
                throw new BusinessRuleException(InvalidCredentialsMessage);

            int? studentId = null;
            if (user.Role == UserRole.Student)
            {
                var student = await _studentRepository.GetByUserIdAsync(user.Id, cancellationToken);
                studentId = student?.Id;
            }

            var token = _tokenGenerator.GenerateToken(user, studentId);

            return new AuthResponseDto(token, user.FullName, user.Role.ToString(), studentId);
        }
    }
}
