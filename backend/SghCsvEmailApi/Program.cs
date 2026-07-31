using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using SghCsvEmailApi.Options;
using SghCsvEmailApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddOptions<GmailOptions>()
    .BindConfiguration(GmailOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<CsvEmailSender>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("gameplay-email", limiterOptions =>
    {
        limiterOptions.PermitLimit = 20;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
    });
});

var app = builder.Build();
app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseRateLimiter();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/gameplay-csv", async (HttpRequest request, CsvEmailSender sender, CancellationToken cancellationToken) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest(new { error = "Expected multipart form data." });

    IFormCollection form = await request.ReadFormAsync(cancellationToken);
    IFormFile? csv = form.Files.GetFile("csv");
    string roomName = form["roomName"].ToString();
    string playedAt = form["playedAt"].ToString();

    if (csv is null || csv.Length == 0 || csv.Length > 5 * 1024 * 1024)
        return Results.BadRequest(new { error = "Attach a CSV file no larger than 5 MB." });

    if (!csv.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest(new { error = "The attachment must be a CSV file." });

    await sender.SendAsync(csv, roomName, playedAt);
    return Results.NoContent();
}).RequireRateLimiting("gameplay-email");

app.Run();
