using MediatR;

namespace SmartGrader.Application.UseCases.Students.ExportStudents;

public record ExportStudentsQuery() : IRequest<byte[]>;
