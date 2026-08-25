using AutoMapper;
using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Common.Interfaces;
using SmartGrader.Application.Dtos.Teacher;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Teachers.CreateTeacher
{
    public class CreateTeacherHandler : IRequestHandler<CreateTeacherCommand, TeacherResponseDto>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasherService _passwordHasher;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CreateTeacherHandler(
            IUserRepository userRepository,
            IPasswordHasherService passwordHasher,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<TeacherResponseDto> Handle(
            CreateTeacherCommand request,
            CancellationToken cancellationToken)
        {
            if (await _userRepository.ExistsByUsernameAsync(request.Dto.Username, cancellationToken))
                throw new UniqueConstraintException("A user with this username already exists.");

            if (await _userRepository.ExistsByEmailAsync(request.Dto.Email, excludingUserId: null, cancellationToken))
                throw new UniqueConstraintException("A user with this email already exists.");

            var user = User.Create(
                request.Dto.Username,
                _passwordHasher.Hash(request.Dto.Password),
                request.Dto.FullName,
                UserRole.Teacher,
                request.Dto.Email);

            await _userRepository.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // ⚠️ בשונה מ-RegisterTeacherHandler שהוחלף כאן — לא נוצר טוקן. המנהלת יוצרת
            // חשבון עבור מישהי אחרת ונשארת מחוברת כמנהלת; החזרת טוקן כאן הייתה מחליפה לה
            // את הזהות באמצע העבודה.
            return _mapper.Map<TeacherResponseDto>(user);
        }
    }
}
