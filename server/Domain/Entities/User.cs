namespace SmartGrader.Domain.Entities
{
    public enum UserRole
    {
        Teacher = 0,
        Student = 1
    }

    public class User
    {
        public int Id { get; private set; }
        public string Email { get; private set; } = "";
        public string PasswordHash { get; private set; } = "";
        public string FullName { get; private set; } = "";
        public UserRole Role { get; private set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        protected User() { }

        public static User Create(string email, string passwordHash, string fullName, UserRole role)
        {
            return new User
            {
                Email = email.Trim().ToLowerInvariant(),
                PasswordHash = passwordHash,
                FullName = fullName.Trim(),
                Role = role,
                CreatedAt = DateTime.UtcNow
            };
        }

        public void SetPasswordHash(string passwordHash)
        {
            PasswordHash = passwordHash;
        }
    }
}
