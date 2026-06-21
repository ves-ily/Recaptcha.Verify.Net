using Recaptcha.Verify.Net.TokenVerification.Client.Models.Response;
using Xunit;

namespace Recaptcha.Verify.Net.Test.VerificationResultValidation;

/// <summary>
/// Focused boundary coverage for the score-threshold check in
/// <c>RecaptchaVerificationResultValidationService</c>, which compares the response score with
/// <c>score &gt;= threshold</c>. Covers the equality boundary, a hair-below value, and a
/// <see cref="float.NaN"/> score.
/// </summary>
public class ScoreBoundaryTest
{
    private const string Action = "login";
    private const float Threshold = 0.5f;

    /// <summary>
    /// A score that equals the threshold satisfies it because the comparison uses <c>&gt;=</c>.
    /// </summary>
    [Fact]
    public void ScoreSatisfies_True_WhenScoreEqualsThreshold()
    {
        var validationService = ValidationServiceFixture.Create(Action, Threshold, null);

        var response = new VerifyResponse { Success = true, Score = Threshold, Action = Action };

        var result = validationService.Validate(response);

        Assert.True(result.ScoreSatisfies);
        Assert.True(result.Success);
    }

    /// <summary>
    /// A score a hair below the threshold (one float ULP) does not satisfy it.
    /// </summary>
    [Fact]
    public void ScoreSatisfies_False_WhenScoreIsJustBelowThreshold()
    {
        var validationService = ValidationServiceFixture.Create(Action, Threshold, null);

        // BitDecrement gives the largest float strictly less than Threshold (one ULP below),
        // guaranteeing the value is below without floating-point formatting surprises.
        var threshold = Threshold;
        var justBelowThreshold = MathF.BitDecrement(threshold);
        Assert.True(justBelowThreshold < threshold);

        var response = new VerifyResponse { Success = true, Score = justBelowThreshold, Action = Action };

        var result = validationService.Validate(response);

        Assert.False(result.ScoreSatisfies);
        Assert.False(result.Success);
    }

    /// <summary>
    /// A v3 response whose score is <see cref="float.NaN"/> never satisfies the threshold:
    /// every comparison involving NaN (including <c>&gt;=</c>) evaluates to <c>false</c>, so
    /// <see cref="ValidationResult.ScoreSatisfies"/> is <c>false</c>.
    /// </summary>
    [Fact]
    public void ScoreSatisfies_False_WhenScoreIsNaN()
    {
        var validationService = ValidationServiceFixture.Create(Action, Threshold, null);

        var response = new VerifyResponse { Success = true, Score = float.NaN, Action = Action };

        var result = validationService.Validate(response);

        // NaN >= threshold is false by IEEE 754, so the score requirement is never met.
        Assert.False(result.ScoreSatisfies);
        Assert.False(result.Success);
    }
}
