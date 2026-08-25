using AutoMapper;
using ClosedXML.Excel;
using MediatR;
using SmartGrader.Application.Common.Exceptions;
using SmartGrader.Application.Common.HebrewDate;
using SmartGrader.Application.Common.Interfaces;
using SmartGrader.Application.Common.Validation;
using SmartGrader.Application.Dtos.Student;
using SmartGrader.Domain.Abstractions;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Students.ImportStudents
{
    public class ImportStudentsHandler
        : IRequestHandler<ImportStudentsCommand, ImportStudentsResultDto>
    {
        private const string InvalidFileMessage = "קובץ לא תקין — יש להעלות קובץ Excel (xlsx)";

        private readonly IStudentRepository _repository;
        private readonly IUserRepository _userRepository;
        private readonly ISchoolClassRepository _classRepository;
        private readonly IPasswordHasherService _passwordHasher;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ImportStudentsHandler(
            IStudentRepository repository,
            IUserRepository userRepository,
            ISchoolClassRepository classRepository,
            IPasswordHasherService passwordHasher,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _userRepository = userRepository;
            _classRepository = classRepository;
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

                // פענוח כיתות מול השנה העברית הנוכחית: קיימת → שיוך, חסרה → יצירה אוטומטית
                var currentYear = HebrewDateConverter.GetCurrentHebrewYear();
                var existingClassIds = new Dictionary<string, int>();
                var newClasses = new Dictionary<string, SchoolClass>();
                var archivedClassNames = new HashSet<string>();

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
                    else if (!existingClassIds.ContainsKey(className) && !newClasses.ContainsKey(className))
                    {
                        if (archivedClassNames.Contains(className))
                            messages.Add("הכיתה נמצאת בארכיון — לא ניתן לשייך אליה תלמידים");
                        else
                        {
                            var found = await _classRepository.GetByNameAndYearAsync(className, currentYear, cancellationToken);
                            if (found is null)
                            {
                                var created = SchoolClass.Create(className, currentYear);
                                await _classRepository.AddAsync(created, cancellationToken);
                                newClasses[className] = created;
                            }
                            else if (found.IsArchived)
                            {
                                archivedClassNames.Add(className);
                                messages.Add("הכיתה נמצאת בארכיון — לא ניתן לשייך אליה תלמידים");
                            }
                            else
                            {
                                existingClassIds[className] = found.Id;
                            }
                        }
                    }

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
                        {
                            messages.Add("יש למלא סיסמה כאשר מוזן שם משתמש");
                        }
                        else
                        {
                            // ⚠️ לא לשכפל כאן את הכללים. הגרסה הקודמת מימשה אותם מחדש והשמיטה
                            // את בדיקת האותיות בעברית — סיסמה שנדחתה בטופס התקבלה דרך הייבוא.
                            var passwordError = PasswordPolicy.GetFailureReason(password);
                            if (passwordError is not null)
                                messages.Add(passwordError);
                        }
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
                        FullName = fullName
                    });

                    if (existingClassIds.TryGetValue(className, out var classId))
                        student.ClassId = classId;
                    else
                        student.Class = newClasses[className];

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
