using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Common.Interfaces;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Teachers.ResetTeacherPassword
{
    public class ResetTeacherPasswordHandler : IRequestHandler<ResetTeacherPasswordCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasherService _passwordHasher;
        private readonly IUnitOfWork _unitOfWork;

        public ResetTeacherPasswordHandler(
            IUserRepository userRepository,
            IPasswordHasherService passwordHasher,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(
            ResetTeacherPasswordCommand request,
            CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);

            if (user is null || user.Role != UserRole.Teacher)
                throw new NotFoundException("Teacher", request.Id);

            user.SetPasswordHash(_passwordHasher.Hash(request.Dto.NewPassword));

            // ⚠️ איפוס מונה הכישלונות והנעילה הוא חלק מהאיפוס, לא תוספת: הסיבה הנפוצה
            // ביותר שמורה פונה למנהלת היא בדיוק חשבון שננעל אחרי חמישה ניסיונות. בלי זה
            // היא מקבלת סיסמה חדשה ועדיין לא נכנסת, בלי שאף אחת מהשתיים תבין למה.
            user.RegisterSuccessfulLogin();

            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
