using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace SmartGrader.Api.Controllers
{
    /// <summary>
    /// Base controller for endpoints that need the authenticated user's identity for
    /// per-teacher ownership scoping. Controllers that don't need ownership (e.g. shared
    /// resources like Students/SchoolClasses, or Admin-only screens) keep extending
    /// <see cref="ControllerBase"/> directly.
    /// </summary>
    [ApiController]
    public abstract class ApiControllerBase : ControllerBase
    {
        protected int CurrentUserId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "0");

        /// <summary>
        /// null = privileged (Admin sees all, no filtering applied); otherwise the current
        /// teacher's user id, to be threaded into every query/command that must be scoped
        /// to "this teacher's own data". There is deliberately no default value on the
        /// TeacherId parameters that consume this — omitting it is a compile error, not a
        /// silent leak.
        /// </summary>
        protected int? OwnerScopeTeacherId => User.IsInRole("Admin") ? null : CurrentUserId;

        /// <summary>
        /// A student may access only her own data: the studentId claim from the token must
        /// match the studentId in the route. Teachers/Admins can access all. Same idiom as
        /// StudentsController.IsAllowedForStudent — promoted here so LessonResultController
        /// can reuse it for the /{studentId}/{lessonId} ownership check.
        /// </summary>
        protected bool IsAllowedForStudent(int studentId)
        {
            if (User.IsInRole("Teacher") || User.IsInRole("Admin"))
                return true;

            var claim = User.FindFirstValue("studentId");
            return claim is not null && int.TryParse(claim, out var ownId) && ownId == studentId;
        }
    }
}
