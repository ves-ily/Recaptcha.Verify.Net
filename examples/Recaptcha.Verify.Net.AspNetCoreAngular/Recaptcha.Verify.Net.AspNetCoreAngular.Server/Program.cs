using OpenTelemetry;
using OpenTelemetry.Trace;
using Recaptcha.Verify.Net.AspNetCoreAngular.Server;
using Recaptcha.Verify.Net.AspNetCoreAngular.Server.Models;
using Recaptcha.Verify.Net.Configuration;
using Recaptcha.Verify.Net.Tracing;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRecaptcha(builder.Configuration.GetSection("Recaptcha"))
    .AddRecaptchaActionArgumentsTokenExtractor(x =>
    {
        if (x.TryGetValue("credentials", out var credentials))
        {
            return (credentials as BaseRecaptchaCredentials)?.RecaptchaToken;
        }

        return null;
    });

builder.Services.AddLogging();

builder.Services.AddControllers();
builder.Services.AddExceptionHandler<RecaptchaExceptionHandler>();
builder.Services.AddProblemDetails();

// Distributed tracing: capture incoming requests, outgoing HTTP calls (the Google
// siteverify request), and the library's Recaptcha.* spans. Exported via OTLP —
// point it at a collector (e.g. http://localhost:4317) to observe the traces.
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource(RecaptchaInstrumentation.ActivitySourceName)
        .AddOtlpExporter());

var app = builder.Build();

app.UseExceptionHandler();

app.UseDefaultFiles();
app.MapStaticAssets();

app.UseAuthorization();

app.MapControllers();

app.Run();
