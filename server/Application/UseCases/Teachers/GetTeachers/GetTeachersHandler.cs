using AutoMapper;
using MediatR;
using SmartGrader.Application.Dtos.Teacher;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Teachers.GetTeachers
{
    public class GetTeachersHandler : IRequestHandler<GetTeachersQuery, IReadOnlyList<TeacherResponseDto>>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILessonRepository _lessonRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IMapper _mapper;

        public GetTeachersHandler(
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

        public async Task<IReadOnlyList<TeacherResponseDto>> Handle(
            GetTeachersQuery request,
            CancellationToken cancellationToken)
        {
            var teachers = await _userRepository.GetByRoleAsync(UserRole.Teacher, cancellationToken);

            var result = new List<TeacherResponseDto>(teachers.Count);

            // שאילתת ספירה לכל מורה. סגל בית ספר הוא עשרות שורות, ולכן זה זול —
            // והספירות האלה הן בדיוק מה שהמנהלת צריכה לראות לפני שהיא לוחצת "מחיקה".
            foreach (var teacher in teachers)
            {
                var lessons = await _lessonRepository.CountByTeacherIdAsync(teacher.Id, cancellationToken);
                var courses = await _courseRepository.CountByTeacherIdAsync(teacher.Id, cancellationToken);

                result.Add(_mapper.Map<TeacherResponseDto>(teacher) with
                {
                    LessonsCount = lessons,
                    CoursesCount = courses
                });
            }

            return result;
        }
    }
}
