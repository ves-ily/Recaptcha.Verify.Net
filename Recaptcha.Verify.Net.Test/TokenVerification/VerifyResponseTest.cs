using System.Text.Json;
using Recaptcha.Verify.Net.TokenVerification.Client.Models.Response;
using Xunit;

namespace Recaptcha.Verify.Net.Test.TokenVerification;

public class VerifyResponseTest
{
    [Fact]
    public void Deserialize_ChallengeTs_WithOffset_PreservesOffset()
    {
        var json = """{"success":true,"challenge_ts":"2024-05-01T12:00:00+03:00"}""";

        var response = JsonSerializer.Deserialize<VerifyResponse>(json)!;

        Assert.True(response.ChallengeTs.HasValue);
        Assert.Equal(TimeSpan.FromHours(3), response.ChallengeTs!.Value.Offset);
        Assert.Equal(2024, response.ChallengeTs!.Value.Year);
    }

    [Fact]
    public void Deserialize_ChallengeTs_Omitted_YieldsNull()
    {
        var json = """{"success":true}""";

        var response = JsonSerializer.Deserialize<VerifyResponse>(json)!;

        Assert.Null(response.ChallengeTs);
    }
}
