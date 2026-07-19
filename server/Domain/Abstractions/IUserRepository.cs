using SmartGrader.Domain.Entities;

namespace SmartGrader.Domain.Abstractions
{
    public interface IUserRepository
    {
        Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
        Task<User?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<bool> ExistsByUsernameAsync(string username, CancellationToken ct = default);
        Task AddAsync(User user, CancellationToken ct = default);
        Task DeleteAsync(User user, CancellationToken ct = default);
    }
}
