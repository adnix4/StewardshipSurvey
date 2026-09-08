using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StewardshipSurvey.Tests.Unit
{
    /// <summary>
    /// Every API controller must deny by default.
    /// <para>
    /// <c>InterestsController</c>, <c>InvolvementsController</c> and
    /// <c>ServiceRolesController</c> carried no class-level <c>[Authorize]</c> and opted in per
    /// action instead. Nothing was actually exposed - every <c>current</c> action had its own
    /// attribute - which is exactly why it survived: the defect is in what happens next, when
    /// an action is added without one and is silently public.
    /// </para>
    /// <para>
    /// That makes it untestable through HTTP: no request behaves differently. It is testable
    /// structurally, which is what this does - and it keeps working for controllers that do not
    /// exist yet, which a request-based test could not.
    /// </para>
    /// </summary>
    public class ApiControllerAuthorizationTests
    {
        private static IEnumerable<Type> ApiControllers() =>
            typeof(Program).Assembly
                .GetTypes()
                .Where(t => t.Namespace == "StewardshipSurvey.Controllers.Api"
                            && typeof(ControllerBase).IsAssignableFrom(t)
                            && !t.IsAbstract);

        [Fact]
        public void Every_api_controller_denies_by_default()
        {
            var unguarded = ApiControllers()
                .Where(t => t.GetCustomAttribute<AuthorizeAttribute>(inherit: true) == null)
                .Select(t => t.Name)
                .OrderBy(n => n)
                .ToList();

            Assert.True(unguarded.Count == 0,
                "These API controllers have no class-level [Authorize], so an action added " +
                "without an attribute would be public: " + string.Join(", ", unguarded) +
                ". Add [Authorize] to the class and [AllowAnonymous] to the actions that are " +
                "deliberately public.");
        }

        [Fact]
        public void The_controllers_this_guards_are_actually_being_found()
        {
            // A reflection test that silently matches nothing passes forever. This pins the
            // count so a namespace rename cannot turn the test above into a no-op.
            var names = ApiControllers().Select(t => t.Name).OrderBy(n => n).ToList();

            Assert.Equal(
                new[]
                {
                    "AuthController", "InterestsController", "InvolvementsController",
                    "MembersController", "ReportsController", "ServiceRolesController"
                },
                names);
        }

        [Fact]
        public void The_public_catalogue_actions_are_the_only_anonymous_ones()
        {
            // The other half: default-deny is only safe if the opt-outs are deliberate. If a
            // new [AllowAnonymous] appears, it should have to be added here on purpose.
            var anonymous = ApiControllers()
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                .Where(m => m.GetCustomAttribute<AllowAnonymousAttribute>() != null)
                .Select(m => $"{m.DeclaringType!.Name}.{m.Name}")
                .OrderBy(n => n)
                .ToList();

            Assert.Equal(
                new[]
                {
                    "AuthController.Login",
                    "AuthController.Refresh",
                    "InterestsController.GetAllInterests",
                    "InvolvementsController.GetAllInvolvements",
                    "ServiceRolesController.GetAllServiceRoles"
                },
                anonymous);
        }
    }
}
