namespace SmartGrader.Application.Dtos.Auth
{
    public record LoginRequestDto(string Email, string Password);

    public record RegisterTeacherRequestDto(string FullName, string Email, string Password);

    public record CreateStudentAccountRequestDto(string FullName, string ClassName, string Email, string Password);

    public record CreateAccountForStudentRequestDto(string Email, string Password);

    public record AuthResponseDto(string Token, string FullName, string Role, int? StudentId);

    public record CurrentUserDto(int UserId, string FullName, string Role, int? StudentId);
}
