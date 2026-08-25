using AutoMapper;
using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Dtos.Teacher;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Teachers.UpdateTeacher
{
    public class UpdateTeacherHandler : IRequestHandler<UpdateTeacherCommand, TeacherResponseDto>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILessonRepository _lessonRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UpdateTeacherHandler(
            IUserRepository userRepository,
            ILessonRepository lessonRepository,
            ICourseRepository courseRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _userRepository = userRepository;
            _lessonRepository = lessonRepository;
            _courseRepository = courseRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<TeacherResponseDto> Handle(
            UpdateTeacherCommand request,
            CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);

            if (user is null || user.Role != UserRole.Teacher)
                throw new NotFoundException("Teacher", request.Id);

            // excludingUserId: המורה שומרת את הטופס בלי לגעת במייל שלה — בלי זה היא
            // מתנגשת עם עצמה ומקבלת 409 על שינוי השם בלבד.
            if (await _userRepository.ExistsByEmailAsync(request.Dto.Email, user.Id, cancellationToken))
                throw new UniqueConstraintException("A user with this email already exists.");

            user.SetFullName(request.Dto.FullName);
            user.SetEmail(request.Dto.Email);

            // GetByIdAsync מחזירה ישות מנותקת (AsNoTracking), ולכן בלי UpdateAsync
            // השינוי פשוט לא נשמר.
            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var lessons = await _lessonRepository.CountByTeacherIdAsync(user.Id, cancellationToken);
            var courses = await _courseRepository.CountByTeacherIdAsync(user.Id, cancellationToken);

            return _mapper.Map<TeacherResponseDto>(user) with
            {
                LessonsCount = lessons,
                CoursesCount = courses
            };
        }
    }
}
