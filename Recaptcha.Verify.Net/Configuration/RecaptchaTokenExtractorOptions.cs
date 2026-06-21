using Microsoft.AspNetCore.Mvc.Filters;

namespace Recaptcha.Verify.Net.Configuration;

/// <summary>
/// Options for configuring token extractors that retrieve reCAPTCHA response tokens from requests.
/// </summary>
/// <remarks>
/// When multiple sources are configured, extractors are registered (and consulted by the token
/// extraction service, which returns the first non-empty token) in this precedence order:
/// <list type="number">
/// <item><see cref="Header"/></item>
/// <item><see cref="Form"/></item>
/// <item><see cref="Query"/></item>
/// <item><see cref="ActionArgument"/> (name-based)</item>
/// <item><see cref="GetResponseTokenFromActionArguments"/> (delegate; wins over <see cref="ActionArgument"/> when both are set — only the delegate is registered)</item>
/// <item><see cref="GetResponseTokenFromExecutingContext"/></item>
/// </list>
/// </remarks>
public class RecaptchaTokenExtractorOptions
{
    /// <summary>
    /// Name of the request header that contains the reCAPTCHA response token.
    /// </summary>
    public string? Header { get; set; }

    /// <summary>
    /// Name of the form field that contains the reCAPTCHA response token.
    /// </summary>
    public string? Form { get; set; }

    /// <summary>
    /// Name of the query parameter that contains the reCAPTCHA response token.
    /// </summary>
    [Obsolete("Do not pass token in query parameters. It is not secure.")]
    public string? Query { get; set; }

    /// <summary>
    /// Name of the action argument that contains the reCAPTCHA response token.
    /// Action arguments are the mapped arguments of the controller method.
    /// </summary>
    public string? ActionArgument { get; set; }

    /// <summary>
    /// Delegate for getting reCAPTCHA response token from action arguments.
    /// Actions arguments are mapped arguments of controller method.
    /// </summary>
    public Func<IDictionary<string, object?>, string?>? GetResponseTokenFromActionArguments { get; set; }

    /// <summary>
    /// Delegate for getting reCAPTCHA response token from executing context.
    /// </summary>
    public Func<ActionExecutingContext, string?>? GetResponseTokenFromExecutingContext { get; set; }
}
