namespace Recaptcha.Verify.Net.VerificationResultValidation;

/// <summary>
/// Service for validation of reCAPTCHA response token verification result.
/// </summary>
public interface IRecaptchaVerificationResultValidationService
{
    /// <summary>
    /// Validates reCAPTCHA response token verification result.
    /// <para>For v3 if not specified takes score threshold and action from options <see cref="RecaptchaOptions"/> .</para>
    /// </summary>
    /// <remarks>
    /// For v3, action matching is an <b>ordinal, case-sensitive comparison with no whitespace trimming</b>
    /// (the expected action and the response action must be byte-for-byte equal). This matches Google
    /// reCAPTCHA v3's case-sensitive action semantics — callers must pass actions with the exact casing
    /// and whitespace expected from the client-side execution. Do not rely on case-insensitivity or
    /// trimming; passing a mismatched-case or padded action will not match.
    /// </remarks>
    /// <param name="response">Result of reCAPTCHA response token verification.</param>
    /// <param name="action">Action that the action from the response should be equal to (exact, case-sensitive match, no trimming).</param>
    /// <param name="score">Score threshold.</param>
    /// <returns>Result of validation of reCAPCTHA token verification.</returns>
    /// <exception cref="EmptyActionException">
    /// This exception is thrown when the action passed in function is empty.
    /// </exception>
    /// <exception cref="MinScoreNotSpecifiedException">
    /// This exception is thrown when minimal score was not specified and request had score value.
    /// </exception>
    ValidationResult Validate(VerifyResponse response, string? action = null, float? score = null);
}
