namespace Recaptcha.Verify.Net.Configuration;

/// <summary>
/// Options for <see cref="VerificationResultValidation.IRecaptchaVerificationResultValidationService"/>.
/// </summary>
public class RecaptchaValidationOptions
{
    /// <summary>
    /// Optional. Action to check for V3 Recaptcha request.
    /// </summary>
    /// <remarks>
    /// Action matching is an ordinal, case-sensitive comparison with no whitespace trimming — the
    /// expected action and the response action must be byte-for-byte equal. This matches Google
    /// reCAPTCHA v3's case-sensitive action semantics, so callers must specify actions with the exact
    /// casing and whitespace expected from the client-side execution.
    /// </remarks>
    public string? Action { get; set; }

    /// <summary>
    /// Optional. Score threshold for V3 Recaptcha (0.0 - 1.0).
    /// </summary>
    public float? ScoreThreshold { get; set; }

    /// <summary>
    /// Optional. Map of actions score thresholds for V3 Recaptcha.
    /// </summary>
    public IReadOnlyDictionary<string, float>? ActionsScoreThresholds { get; set; }
}
