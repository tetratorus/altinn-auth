namespace Altinn.AccessMgmt.FFB.Config;

/// <summary>
/// OpenID Connect settings for signing in operators (e.g. Entra ID).
/// The app refuses to start unless <see cref="Authority"/> and <see cref="ClientId"/> are set.
/// </summary>
public class AuthenticationConfig
{
    public const string SectionName = "Authentication";

    /// <summary>
    /// Issuer URL, e.g. https://login.microsoftonline.com/{tenant-id}/v2.0.
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Optional for public clients using PKCE only.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    public string CallbackPath { get; set; } = "/signin-oidc";

    /// <summary>
    /// Claim type that carries the operator's roles. Entra ID app roles are issued as "roles".
    /// </summary>
    public string RoleClaimType { get; set; } = "roles";

    /// <summary>
    /// Role every signed-in user must hold to use the tool. Empty means any authenticated user
    /// from the configured authority is allowed.
    /// </summary>
    public string RequiredRole { get; set; } = string.Empty;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Authority) || string.IsNullOrWhiteSpace(ClientId))
        {
            throw new InvalidOperationException(
                $"'{SectionName}:Authority' and '{SectionName}:ClientId' must be configured. " +
                "The tool holds live database credentials and will not start without authentication.");
        }

        if (!Uri.TryCreate(Authority, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException($"'{SectionName}:Authority' must be an absolute https URL.");
        }
    }
}
