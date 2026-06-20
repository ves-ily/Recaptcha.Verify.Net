namespace Recaptcha.Verify.Net.Tracing;

/// <summary>
/// Distributed-tracing instrumentation for reCAPTCHA verification.
/// <para>
/// Subscribe to <see cref="ActivitySource"/> to collect spans for token extraction,
/// verification (the outbound request to Google), and validation, e.g. with OpenTelemetry:
/// <code>.AddSource(RecaptchaInstrumentation.ActivitySourceName)</code>.
/// </para>
/// </summary>
public static class RecaptchaInstrumentation
{
    /// <summary>
    /// The <see cref="ActivitySource.Name"/> consumers subscribe to.
    /// </summary>
    public const string ActivitySourceName = "Recaptcha.Verify.Net";

    /// <summary>
    /// The <see cref="ActivitySource"/> that emits reCAPTCHA verification activities.
    /// </summary>
    public static ActivitySource ActivitySource { get; } = new(ActivitySourceName);
}

internal static class RecaptchaActivityExtensions
{
    /// <summary>
    /// Marks the activity as failed and records the exception on it (OpenTelemetry
    /// exception semantic conventions: <c>exception.type</c>, <c>exception.message</c>,
    /// <c>exception.stacktrace</c>).
    /// </summary>
    public static void SetError(this Activity activity, Exception exception)
    {
        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
        {
            ["exception.type"] = exception.GetType().FullName,
            ["exception.message"] = exception.Message,
            ["exception.stacktrace"] = exception.StackTrace,
        }));
    }
}
