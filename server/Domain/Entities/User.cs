namespace SmartGrader.Domain.Entities
{
    public enum UserRole
    {
        Teacher = 0,
        Student = 1,
        Admin = 2
    }

    public class User
    {
        public int Id { get; private set; }
        public string Username { get; private set; } = "";
        public string PasswordHash { get; private set; } = "";
        public string FullName { get; private set; } = "";
        public UserRole Role { get; private set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        protected User() { }

        public static User Create(string username, string passwordHash, string fullName, UserRole role)
        {
            return new User
            {
                Username = username.Trim().ToLowerInvariant(),
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
