using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Recaptcha.Verify.Net.Configuration;
using Recaptcha.Verify.Net.TokenVerification;
using Recaptcha.Verify.Net.TokenVerification.Client;
using Recaptcha.Verify.Net.TokenVerification.Client.Models.Response;
using Xunit;

namespace Recaptcha.Verify.Net.Test.TokenVerification;

/// <summary>
/// End-to-end coverage for a custom <see cref="RecaptchaVerificationOptions.BaseUrl"/>: drives the request
/// through the real DI registration (<see cref="ConfigurationExtensions.AddRecaptcha"/>, which normalizes
/// the base URL) and <see cref="IRecaptchaVerificationService.VerifyAsync"/>, then asserts the outgoing
/// request targets <c>&lt;base&gt;/siteverify</c> and carries the expected form fields.
/// </summary>
public class CustomBaseUrlTest
{
    private const string SecretKey = "test-secret";
    private const string ResponseToken = "test-response-token";
    private const string RemoteIp = "203.0.113.42";
    private const string CustomBaseUrl = "https://recaptcha.local/api";

    [Fact]
    public async Task VerifyAsync_PostsToCustomBaseUrlSiteVerify_WithExpectedFormFields()
    {
        var handler = new CapturingHandler();

        var services = new ServiceCollection();
        services.AddLogging();
        // Registers the typed client with the normalized BaseAddress derived from BaseUrl
        // (ConfigureService appends a trailing slash when missing).
        services.AddRecaptcha(o =>
        {
            o.Verification.SecretKey = SecretKey;
            o.Verification.BaseUrl = CustomBaseUrl;
        });
        // Attach the capturing primary handler to the same typed client so the real HTTP pipeline
        // (and the BaseAddress set by AddRecaptcha) is exercised end to end. Re-specifying the
        // RecaptchaClient implementation keeps the typed-client binding intact while adding the
        // handler to the shared named options for IRecaptchaClient.
        services.AddHttpClient<IRecaptchaClient, RecaptchaClient>()
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        using var provider = services.BuildServiceProvider();
        var verificationService = provider.GetRequiredService<IRecaptchaVerificationService>();

        var result = await verificationService.VerifyAsync(ResponseToken, RemoteIp);

        Assert.NotNull(result);
        Assert.True(result.Success);

        var captured = Assert.Single(handler.Requests);
        // BaseUrl "https://recaptcha.local/api" is normalized to "https://recaptcha.local/api/"
        // and the client posts to the relative "siteverify" segment.
        Assert.Equal("https://recaptcha.local/api/siteverify", captured.RequestUri?.AbsoluteUri);

        // The client posts application/x-www-form-urlencoded (FormUrlEncodedContent).
        var formBody = await captured.Content!.ReadAsStringAsync();
        var form = System.Web.HttpUtility.ParseQueryString(formBody);
        Assert.Equal(SecretKey, form["secret"]);
        Assert.Equal(ResponseToken, form["response"]);
        Assert.Equal(RemoteIp, form["remoteip"]);
    }

    /// <summary>
    /// Captures every request sent through the handler and returns a successful v3 verification response.
    /// </summary>
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new VerifyResponse { Success = true, Score = 1.0f, Action = "login" })
            };

            return Task.FromResult(response);
        }
    }
}
