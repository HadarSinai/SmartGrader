using AutoMapper;
using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Dtos.Teacher;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Teachers.GetTeacherById
{
    public class GetTeacherByIdHandler : IRequestHandler<GetTeacherByIdQuery, TeacherResponseDto>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILessonRepository _lessonRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IMapper _mapper;

        public GetTeacherByIdHandler(
            IUserRepository userRepository,
            ILessonRepository lessonRepository,
            ICourseRepository courseRepository,
            IMapper mapper)
        {
            _userRepository = userRepository;
            _lessonRepository = lessonRepository;
            _courseRepository = courseRepository;
            _mapper = mapper;
        }

        public async Task<TeacherResponseDto> Handle(
            GetTeacherByIdQuery request,
            CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);

            if (user is null)
                throw new NotFoundException("Teacher", request.Id);

            // מסך המורות אינו חלון אל שאר המשתמשות. משתמשת קיימת שאינה מורה היא 404 כאן
            // ולא "מורה בלי שיעורים".
            if (user.Role != UserRole.Teacher)
                throw new NotFoundException("Teacher", request.Id);

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
