using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Recaptcha.Verify.Net.TokenVerification;

/// <inheritdoc />
/// <summary>
/// Recaptcha verification service constructor.
/// </summary>
/// <param name="options">Recaptcha verification options.</param>
/// <param name="recaptchaClient">Recaptcha client.</param>
/// <param name="logger">Logger.</param>
internal class RecaptchaVerificationService(IOptions<RecaptchaVerificationOptions> options, IRecaptchaClient recaptchaClient, ILogger<RecaptchaVerificationService> logger) : IRecaptchaVerificationService
{
    private readonly RecaptchaVerificationOptions options = options.Value;

    /// <inheritdoc />
    public async Task<VerifyResponse> VerifyAsync(string response, string? remoteIp = null, CancellationToken cancellationToken = default)
    {
        using var activity = RecaptchaInstrumentation.ActivitySource.StartActivity("Recaptcha.Verify", ActivityKind.Client);
        try
        {
            if (string.IsNullOrWhiteSpace(options.SecretKey))
            {
                throw new SecretKeyNotSpecifiedException();
            }

            if (string.IsNullOrWhiteSpace(response))
            {
                throw new EmptyCaptchaAnswerException();
            }

            var request = new VerifyRequest
            {
                Secret = options.SecretKey,
                Response = response,
                RemoteIp = remoteIp
            };

            logger.SendingRequest(request);

            VerifyResponse result;

            try
            {
                result = await recaptchaClient.VerifyAsync(request, cancellationToken);
            }
            catch (Exception e)
            {
                throw new VerifyRequestException(e);
            }

            logger.RequestCompleted(result);

            if (result.Action is not null)
            {
                activity?.SetTag("recaptcha.action", result.Action);
            }
            activity?.SetTag("recaptcha.success", result.Success);
            if (result.Score.HasValue)
            {
                activity?.SetTag("recaptcha.score", result.Score.GetValueOrDefault());
            }

            activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (Exception e)
        {
            activity?.SetError(e);
            throw;
        }
    }
}