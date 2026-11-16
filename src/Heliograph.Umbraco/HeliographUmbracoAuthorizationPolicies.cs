namespace Heliograph.Umbraco;

/// <summary>The authorization policies Heliograph.Umbraco's endpoints name, so that none of them names a sign-in scheme.</summary>
public static class HeliographUmbracoAuthorizationPolicies
{
    /// <summary>
    /// Required to read, change, preview or publish correspondence. The package registers it: a backoffice user
    /// passes when they belong to one of <see cref="HeliographUmbracoOptions.AdminGroupAliases"/>.
    /// </summary>
    /// <remarks>
    /// Correspondence sits under the backoffice's Settings section, so who sees it in the menu is decided by a user
    /// group's allowed sections. That is navigation only. Without this policy, any signed-in backoffice user could
    /// call the endpoints directly.
    /// </remarks>
    public const string CorrespondenceAdmin = "Heliograph:CorrespondenceAdmin";
}
