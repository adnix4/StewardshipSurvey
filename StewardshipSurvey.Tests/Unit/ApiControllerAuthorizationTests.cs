using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
        public void Every_api_controller_is_bearer_only()
        {
            // A cookie is attached automatically by the browser and nothing here checks an
            // antiforgery token, so a controller that accepts one is a cross-site write away
            // from being a problem. Naming the scheme is what keeps that shut, and forgetting
            // to name it on a new controller is silent - hence this test rather than a request.
            var wrong = ApiControllers()
                .Select(t => new { t.Name, Attr = t.GetCustomAttribute<AuthorizeAttribute>(inherit: true) })
                .Where(x => x.Attr == null
                            || x.Attr.AuthenticationSchemes != JwtBearerDefaults.AuthenticationScheme)
                .Select(x => x.Name)
                .OrderBy(n => n)
                .ToList();

            Assert.True(wrong.Count == 0,
                "These API controllers do not restrict themselves to the bearer scheme, so they " +
                "would accept the Identity cookie: " + string.Join(", ", wrong) +
                ". Add AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme to the " +
                "class-level [Authorize].");
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
