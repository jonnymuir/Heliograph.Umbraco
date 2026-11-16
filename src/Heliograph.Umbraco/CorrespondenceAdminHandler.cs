using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Security;

namespace Heliograph.Umbraco;

/// <summary>The requirement behind <see cref="HeliographUmbracoAuthorizationPolicies.CorrespondenceAdmin"/>.</summary>
public sealed class CorrespondenceAdminRequirement : IAuthorizationRequirement;

/// <summary>
/// Lets a backoffice user through when they belong to one of <see cref="HeliographUmbracoOptions.AdminGroupAliases"/>.
/// Anyone else, signed in or not, is refused: the handler never succeeds by default.
/// </summary>
public sealed class CorrespondenceAdminHandler(
    IBackOfficeSecurityAccessor securityAccessor, IOptions<HeliographUmbracoOptions> options)
    : AuthorizationHandler<CorrespondenceAdminRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, CorrespondenceAdminRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);

        var groups = securityAccessor.BackOfficeSecurity?.CurrentUser?.Groups ?? [];
        var allowed = options.Value.AdminGroupAliases ?? [];
        if (groups.Any(group => allowed.Contains(group.Alias, StringComparer.OrdinalIgnoreCase)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
