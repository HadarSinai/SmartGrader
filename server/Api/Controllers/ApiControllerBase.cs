using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace SmartGrader.Api.Controllers
{
    /// <summary>
    /// Base controller for endpoints that need the authenticated user's identity for
    /// per-teacher ownership scoping. Controllers that don't need ownership (e.g. Admin-only
    /// screens) keep extending <see cref="ControllerBase"/> directly.
    /// </summary>
    [ApiController]
    public abstract class ApiControllerBase : ControllerBase
    {
        protected int CurrentUserId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "0");

        /// <summary>Teacher or Admin — i.e. not a student.</summary>
        protected bool IsPrivilegedUser => User.IsInRole("Teacher") || User.IsInRole("Admin");

        /// <summary>
        /// null = privileged (Admin sees all, no filtering applied); otherwise the current
        /// teacher's user id, to be threaded into every query/command that must be scoped
        /// to "this teacher's own data". There is deliberately no default value on the
        /// TeacherId parameters that consume this — omitting it is a compile error, not a
        /// silent leak.
        /// </summary>
        protected int? OwnerScopeTeacherId => User.IsInRole("Admin") ? null : CurrentUserId;

        /// <summary>
        /// The teacher-ownership filter for endpoints a student shares with a teacher. It must
        /// be null for a student — her CurrentUserId is a *user* id, not a teacher id, and would
        /// collapse every lesson to 404. Her own scoping is carried by <see cref="StudentIdClaim"/>
        /// instead; see <c>LessonAccess.GetAccessibleOrThrowAsync</c>.
        /// </summary>
        protected int? TeacherIdForSharedRead => IsPrivilegedUser ? OwnerScopeTeacherId : null;

        /// <summary>
        /// The studentId claim from the token, or null when absent/unparsable. Never read the
        /// student id from the route or body for scoping — only from here.
        /// </summary>
        protected int? StudentIdClaim
        {
            get
            {
                var claim = User.FindFirstValue("studentId");
                return claim is not null && int.TryParse(claim, out var id) ? id : null;
            }
        }

        /// <summary>
        /// Resolves the scope for an endpoint that both teachers and students call. Returns false
        /// when the caller is neither — a Student login with no linked student record has no
        /// scope at all, and must be rejected rather than read unscoped.
        /// </summary>
        protected bool TryResolveSharedReadScope(out int? teacherId, out int? studentId)
        {
            teacherId = TeacherIdForSharedRead;
            studentId = null;

            if (IsPrivilegedUser)
                return true;

            studentId = StudentIdClaim;
            return studentId.HasValue;
        }

        /// <summary>
        /// A student may access only her own data: the studentId claim from the token must
        /// match the studentId in the route. Teachers/Admins can access all — their own
        /// scoping is applied downstream by lesson ownership.
        /// </summary>
        protected bool IsAllowedForStudent(int studentId)
        {
            if (IsPrivilegedUser)
                return true;

            return StudentIdClaim == studentId;
        }
    }
}
