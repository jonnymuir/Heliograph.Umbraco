namespace Heliograph.Umbraco;

/// <summary>How a site sets Heliograph.Umbraco up. Bound from the <c>Heliograph</c> configuration section.</summary>
public sealed class HeliographUmbracoOptions
{
    /// <summary>The configuration section these options are read from.</summary>
    public const string SectionName = "Heliograph";

    /// <summary>
    /// The backoffice user groups whose members may work on correspondence. It starts as the built-in
    /// Administrators group; a site adds the groups its content designers are in. An empty list lets nobody in.
    /// </summary>
#pragma warning disable CA1819 // Bound from configuration, which needs a settable array.
    public string[] AdminGroupAliases { get; set; } = [global::Umbraco.Cms.Core.Constants.Security.AdminGroupAlias];
#pragma warning restore CA1819
}
