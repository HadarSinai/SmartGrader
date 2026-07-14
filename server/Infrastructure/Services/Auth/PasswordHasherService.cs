using Microsoft.AspNetCore.Identity;
using SmartGrader.Application.Common.Interfaces;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Infrastructure.Services.Auth
{
    public class PasswordHasherService : IPasswordHasherService
    {
        private readonly PasswordHasher<User> _hasher = new();

        public string Hash(string password)
        {
            // PasswordHasher does not use the user instance for hashing — null! is safe here
            return _hasher.HashPassword(null!, password);
        }

        public bool Verify(string passwordHash, string providedPassword)
        {
            var result = _hasher.VerifyHashedPassword(null!, passwordHash, providedPassword);
            return result is PasswordVerificationResult.Success
                or PasswordVerificationResult.SuccessRehashNeeded;
        }
    }
}
