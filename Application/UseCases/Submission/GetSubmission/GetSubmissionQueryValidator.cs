using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartGrader.Application.UseCases.Submissions.GetSubmissions
{
    public class GetSubmissionsQueryValidator : AbstractValidator<GetSubmissionsQuery>
    {
        public GetSubmissionsQueryValidator()
        {
        }
    }
}
