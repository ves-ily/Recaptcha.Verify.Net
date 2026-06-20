using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Recaptcha.Verify.Net.Configuration;
using Recaptcha.Verify.Net.Exceptions.Configuration;
using Recaptcha.Verify.Net.Exceptions.Processing;
using Recaptcha.Verify.Net.Test.TokenExtraction;
using Recaptcha.Verify.Net.TokenExtraction;
using Recaptcha.Verify.Net.TokenVerification;
using Recaptcha.Verify.Net.TokenVerification.Client;
using Recaptcha.Verify.Net.TokenVerification.Client.Models.Request;
using Recaptcha.Verify.Net.TokenVerification.Client.Models.Response;
using Recaptcha.Verify.Net.Tracing;
using Recaptcha.Verify.Net.VerificationResultValidation;
using Xunit;

namespace Recaptcha.Verify.Net.Test.Tracing;

public class TracingTest
{
    private const string SecretKey = "test-secret";

    [Fact]
    public async Task Verify_EmitsClientActivity_WithTags()
    {
        using var capture = new ActivityCapture();
        var clientMock = new Mock<IRecaptchaClient>();
        clientMock
            .Setup(c => c.VerifyAsync(It.IsAny<VerifyRequest>(), default))
            .ReturnsAsync(new VerifyResponse { Success = true, Score = 0.9f, Action = "login" });
        var service = new RecaptchaVerificationService(
            Options.Create(new RecaptchaVerificationOptions { SecretKey = SecretKey }),
            clientMock.Object,
            NullLoggerFactory.Instance.CreateLogger<RecaptchaVerificationService>());

        await service.VerifyAsync("token");

        var activity = Assert.Single(capture.Activities);
        Assert.Equal("Recaptcha.Verify", activity.OperationName);
        Assert.Equal(ActivityKind.Client, activity.Kind);
        Assert.Equal(ActivityStatusCode.Ok, activity.Status);
        Assert.Equal("login", activity.GetTagItem("recaptcha.action"));
        Assert.True((bool)activity.GetTagItem("recaptcha.success")!);
        Assert.Equal(0.9f, (float)activity.GetTagItem("recaptcha.score")!);
    }

    [Fact]
    public async Task Verify_RecordsError_WhenRequestThrows()
    {
        using var capture = new ActivityCapture();
        var clientMock = new Mock<IRecaptchaClient>();
        clientMock
            .Setup(c => c.VerifyAsync(It.IsAny<VerifyRequest>(), default))
            .ThrowsAsync(new HttpRequestException());
        var service = new RecaptchaVerificationService(
            Options.Create(new RecaptchaVerificationOptions { SecretKey = SecretKey }),
            clientMock.Object,
            NullLoggerFactory.Instance.CreateLogger<RecaptchaVerificationService>());

        await Assert.ThrowsAsync<VerifyRequestException>(() => service.VerifyAsync("token"));

        var activity = Assert.Single(capture.Activities);
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Contains(activity.Events, e => e.Name == "exception");
    }

    [Fact]
    public void Validate_EmitsActivity_WithTags()
    {
        using var capture = new ActivityCapture();
        var service = new RecaptchaVerificationResultValidationService(
            Options.Create(new RecaptchaValidationOptions { Action = "login", ScoreThreshold = 0.5f }),
            NullLoggerFactory.Instance.CreateLogger<RecaptchaVerificationResultValidationService>());

        service.Validate(new VerifyResponse { Success = true, Score = 0.9f, Action = "login" });

        var activity = Assert.Single(capture.Activities);
        Assert.Equal("Recaptcha.Validate", activity.OperationName);
        Assert.Equal(ActivityKind.Internal, activity.Kind);
        Assert.Equal(ActivityStatusCode.Ok, activity.Status);
        Assert.Equal("login", activity.GetTagItem("recaptcha.action"));
        Assert.Equal(0.5f, (float)activity.GetTagItem("recaptcha.score_threshold")!);
        Assert.True((bool)activity.GetTagItem("recaptcha.action_matches")!);
        Assert.True((bool)activity.GetTagItem("recaptcha.score_satisfies")!);
    }

    [Fact]
    public void Extract_EmitsActivity_WithExtractorCount()
    {
        using var capture = new ActivityCapture();
        var extractor = new Mock<IRecaptchaTokenExtractor>();
        extractor
            .Setup(e => e.GetToken(It.IsAny<ActionExecutingContext>()))
            .Returns("token");
        var service = new RecaptchaTokenExtractionService([extractor.Object]);

        service.GetToken(ActionExecutingContextFixture.CreateActionExecutingContext());

        var activity = Assert.Single(capture.Activities);
        Assert.Equal("Recaptcha.ExtractToken", activity.OperationName);
        Assert.Equal(ActivityKind.Internal, activity.Kind);
        Assert.Equal(ActivityStatusCode.Ok, activity.Status);
        Assert.Equal(1, (int)activity.GetTagItem("recaptcha.extractors.count")!);
    }

    [Fact]
    public void Extract_RecordsError_WhenNoExtractors()
    {
        using var capture = new ActivityCapture();
        var service = new RecaptchaTokenExtractionService([]);

        Assert.Throws<TokenExtractorNotFound>(() => service.GetToken(ActionExecutingContextFixture.CreateActionExecutingContext()));

        var activity = Assert.Single(capture.Activities);
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Contains(activity.Events, e => e.Name == "exception");
    }

    /// <summary>
    /// Subscribes to <see cref="RecaptchaInstrumentation.ActivitySource"/> and collects started activities.
    /// </summary>
    private sealed class ActivityCapture : IDisposable
    {
        private readonly ActivityListener _listener;
        public List<Activity> Activities { get; } = [];

        public ActivityCapture()
        {
            _listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == RecaptchaInstrumentation.ActivitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            };
            _listener.ActivityStarted += activity => Activities.Add(activity);
            ActivitySource.AddActivityListener(_listener);
        }

        public void Dispose() => _listener.Dispose();
    }
}
