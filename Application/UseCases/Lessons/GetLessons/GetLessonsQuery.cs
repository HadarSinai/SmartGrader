using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.UseCases.Lessons.GetLessons
{
    public record GetLessonsQuery() : IRequest<IReadOnlyList<Lesson>>;
}


