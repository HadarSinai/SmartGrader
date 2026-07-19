namespace SmartGrader.Application.Dtos.Auth
{
    public record LoginRequestDto(string Username, string Password);

    public record RegisterTeacherRequestDto(string FullName, string Username, string Password);

    public record CreateStudentAccountRequestDto(string FullName, string ClassName, string Username, string Password);

    public record CreateAccountForStudentRequestDto(string Username, string Password);

    public record AuthResponseDto(string Token, string FullName, string Role, int? StudentId);

    public record CurrentUserDto(int UserId, string FullName, string Role, int? StudentId);
}
