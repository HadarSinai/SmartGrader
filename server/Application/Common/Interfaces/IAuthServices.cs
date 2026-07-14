using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.Common.Interfaces
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(User user, int? studentId);
    }

    public interface IPasswordHasherService
    {
        string Hash(string password);
        bool Verify(string passwordHash, string providedPassword);
    }
}
