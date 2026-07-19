using AutoMapper;
using ClosedXML.Excel;
using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Common.Interfaces;
using SmartGrader.Application.Dtos.Student;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;
using System.Text.RegularExpressions;

namespace SmartGrader.Application.UseCases.Students.ImportStudents
{
    public class ImportStudentsHandler
        : IRequestHandler<ImportStudentsCommand, ImportStudentsResultDto>
    {
        private const string InvalidFileMessage = "קובץ לא תקין — יש להעלות קובץ Excel (xlsx)";

        private readonly IStudentRepository _repository;
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasherService _passwordHasher;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ImportStudentsHandler(
            IStudentRepository repository,
            IUserRepository userRepository,
            IPasswordHasherService passwordHasher,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ImportStudentsResultDto> Handle(
            ImportStudentsCommand request,
            CancellationToken cancellationToken)
        {
            XLWorkbook workbook;
            try
            {
                workbook = new XLWorkbook(request.FileStream);
            }
            catch (Exception)
            {
                throw new BusinessRuleException(InvalidFileMessage);
            }

            using (workbook)
            {
                var ws = workbook.Worksheets.FirstOrDefault();
                if (ws is null)
                    throw new BusinessRuleException(InvalidFileMessage);

                var createdCount = 0;
                var errors = new List<ImportRowErrorDto>();
                var usernamesInFile = new HashSet<string>();

                foreach (var row in ws.RowsUsed().Skip(1))
                {
                    var fullName = row.Cell(1).GetString().Trim();
                    var className = row.Cell(2).GetString().Trim();
                    var username = row.Cell(3).GetString().Trim();
                    var password = row.Cell(4).GetString().Trim();
                    var rowNumber = row.RowNumber();

                    // Skip fully-empty rows silently
                    if (string.IsNullOrEmpty(fullName) && string.IsNullOrEmpty(className)
                        && string.IsNullOrEmpty(username) && string.IsNullOrEmpty(password))
                        continue;

                    var messages = new List<string>();

                    if (string.IsNullOrEmpty(fullName))
                        messages.Add("שם מלא הוא שדה חובה");
                    else if (fullName.Length > 100)
                        messages.Add("שם מלא ארוך מדי (מקסימום 100 תווים)");

                    if (string.IsNullOrEmpty(className))
                        messages.Add("כיתה היא שדה חובה");
                    else if (className.Length > 50)
                        messages.Add("כיתה ארוכה מדי (מקסימום 50 תווים)");

                    var hasAccount = !string.IsNullOrEmpty(username) || !string.IsNullOrEmpty(password);
                    string normalizedUsername = "";

                    if (hasAccount)
                    {
                        normalizedUsername = username.ToLowerInvariant();

                        if (string.IsNullOrEmpty(username))
                            messages.Add("יש למלא שם משתמש כאשר מוזנת סיסמה");
                        else if (username.Length < 3)
                            messages.Add("שם משתמש קצר מדי (מינימום 3 תווים)");
                        else if (username.Length > 50)
                            messages.Add("שם משתמש ארוך מדי (מקסימום 50 תווים)");
                        else if (username.Contains(' '))
                            messages.Add("שם משתמש לא יכול להכיל רווחים");
                        else if (usernamesInFile.Contains(normalizedUsername))
                            messages.Add("שם המשתמש מופיע יותר מפעם אחת בקובץ");
                        else if (await _userRepository.ExistsByUsernameAsync(username, cancellationToken))
                            messages.Add("שם המשתמש כבר קיים במערכת");

                        if (string.IsNullOrEmpty(password))
                            messages.Add("יש למלא סיסמה כאשר מוזן שם משתמש");
                        else if (password.Length < 8
                            || !Regex.IsMatch(password, "[A-Z]")
                            || !Regex.IsMatch(password, "[a-z]")
                            || !Regex.IsMatch(password, "[0-9]"))
                            messages.Add("סיסמה חייבת להכיל לפחות 8 תווים, אות גדולה, אות קטנה וספרה");
                    }

                    if (messages.Count > 0)
                    {
                        errors.Add(new ImportRowErrorDto
                        {
                            RowNumber = rowNumber,
                            Message = string.Join("; ", messages)
                        });
                        continue;
                    }

                    var student = _mapper.Map<Student>(new CreateStudentRequestDto
                    {
                        FullName = fullName,
                        ClassName = className
                    });

                    if (hasAccount)
                    {
                        var user = User.Create(
                            username,
                            _passwordHasher.Hash(password),
                            fullName,
                            UserRole.Student);

                        student.User = user;
                        await _userRepository.AddAsync(user, cancellationToken);
                        usernamesInFile.Add(normalizedUsername);
                    }

                    await _repository.AddAsync(student, cancellationToken);
                    createdCount++;
                }

                if (createdCount > 0)
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                return new ImportStudentsResultDto
                {
                    CreatedCount = createdCount,
                    Errors = errors
                };
            }
        }
    }
}
