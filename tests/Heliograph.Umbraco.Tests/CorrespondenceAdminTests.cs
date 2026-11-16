using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Moq;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;

namespace Heliograph.Umbraco.Tests;

/// <summary>Who may work on correspondence: members of the groups the site names, and nobody else.</summary>
public class CorrespondenceAdminTests
{
    [Fact]
    public async Task An_administrator_is_let_in_by_default()
    {
        (await Decide(new HeliographUmbracoOptions(), "admin")).Should().BeTrue();
    }

    [Fact]
    public async Task A_backoffice_user_outside_the_named_groups_is_refused()
    {
        (await Decide(new HeliographUmbracoOptions(), "editor", "writer")).Should().BeFalse();
    }

    [Fact]
    public async Task A_site_can_let_its_content_designers_in()
    {
        var options = new HeliographUmbracoOptions { AdminGroupAliases = ["admin", "contentDesigners"] };

        (await Decide(options, "ContentDesigners")).Should().BeTrue();
    }

    [Fact]
    public async Task Someone_who_is_not_a_backoffice_user_is_refused()
    {
        (await Decide(new HeliographUmbracoOptions(), user: null)).Should().BeFalse();
    }

    [Fact]
    public async Task An_empty_list_of_groups_lets_nobody_in()
    {
        (await Decide(new HeliographUmbracoOptions { AdminGroupAliases = [] }, "admin")).Should().BeFalse();
    }

    private static Task<bool> Decide(HeliographUmbracoOptions options, params string[] groups) => Decide(options, User(groups));

    private static async Task<bool> Decide(HeliographUmbracoOptions options, IUser? user)
    {
        var security = new Mock<IBackOfficeSecurity>();
        security.SetupGet(s => s.CurrentUser).Returns(user);
        var accessor = new Mock<IBackOfficeSecurityAccessor>();
        accessor.SetupGet(a => a.BackOfficeSecurity).Returns(security.Object);
        var requirement = new CorrespondenceAdminRequirement();
        var context = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(new ClaimsIdentity("test")), resource: null);

        await new CorrespondenceAdminHandler(accessor.Object, Options.Create(options)).HandleAsync(context);

        return context.HasSucceeded;
    }

    private static IUser User(string[] groups)
    {
        var user = new Mock<IUser>();
        user.SetupGet(u => u.Groups).Returns([.. groups.Select(alias =>
        {
            var group = new Mock<IReadOnlyUserGroup>();
            group.SetupGet(g => g.Alias).Returns(alias);
            return group.Object;
        })]);
        return user.Object;
    }
}
