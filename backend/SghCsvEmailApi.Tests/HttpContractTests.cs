using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SghCsvEmailApi.Options;
using SghCsvEmailApi.Services;
using Xunit;

namespace SghCsvEmailApi.Tests;

public class HttpContractTests
{
    private sealed class Sender : ICsvEmailSender
    {
        public int Calls;
        public byte[]? Content;
        public Func<CancellationToken, Task>? BeforeComplete;
        public async Task SendAsync(IFormFile csv, string room, string date, CancellationToken token = default)
        {
            Calls++;
            using var stream = new MemoryStream();
            await csv.CopyToAsync(stream, token);
            Content = stream.ToArray();
            if (BeforeComplete != null) await BeforeComplete(token);
        }
    }

    // Minimal-host startup can dispose its services before WebApplicationFactory's
    // deferred host observes the original exception. Capture that exception at the
    // host boundary so a bare disposal error cannot satisfy a validation test.
    private sealed class StartupFailureProbe : ILoggerProvider, ILogger
    {
        public readonly TaskCompletionSource<OptionsValidationException> Failure =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ILogger CreateLogger(string categoryName) => this;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public void Dispose() { }
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? error,
            Func<TState, Exception?, string> formatter)
        {
            if (error is OptionsValidationException validation && validation.OptionsType == typeof(GmailOptions))
                Failure.TrySetResult(validation);
        }
    }

    private sealed class Host : WebApplicationFactory<Program>
    {
        public readonly Sender Mail = new();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("https_port", "443");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ICsvEmailSender>();
                services.AddSingleton<ICsvEmailSender>(Mail);
            });
            builder.UseSetting("Gmail:SenderEmail", "sender@example.invalid");
            builder.UseSetting("Gmail:RecipientEmail", "receiver@example.invalid");
            builder.UseSetting("Gmail:AppPassword", "synthetic-not-a-password");
        }
        public HttpClient Client() => CreateClient(new WebApplicationFactoryClientOptions
        { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
    }

    private static MultipartFormDataContent Upload(int size, string filename = "synthetic.csv", string field = "csv")
    {
        var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(new byte[size]), field, filename);
        form.Add(new StringContent("synthetic-room"), "roomName");
        form.Add(new StringContent("2026-11-02T00:00:00Z"), "playedAt");
        return form;
    }

    [Fact]
    public async Task HealthAndHttpsRedirectAreRealPipelineResponses()
    {
        using var host = new Host(); using var client = host.Client();
        using var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("ok", await response.Content.ReadAsStringAsync());
        Assert.Equal(0, host.Mail.Calls);
        using var insecure = host.CreateClient(new WebApplicationFactoryClientOptions
        { BaseAddress = new Uri("http://localhost"), AllowAutoRedirect = false });
        using var redirect = await insecure.GetAsync("/health");
        Assert.Equal(HttpStatusCode.TemporaryRedirect, redirect.StatusCode);
        Assert.Equal("https", redirect.Headers.Location!.Scheme);
    }

    [Theory]
    [InlineData(0, "empty.csv", "csv")]
    [InlineData(5242881, "too-large.csv", "csv")]
    [InlineData(1, "wrong.txt", "csv")]
    [InlineData(1, "missing.csv", "other")]
    public async Task InvalidUploadNeverReachesMail(int size, string name, string field)
    {
        using var host = new Host(); using var client = host.Client(); using var form = Upload(size, name, field);
        using var result = await client.PostAsync("/api/gameplay-csv", form);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode); Assert.Equal(0, host.Mail.Calls);
    }

    [Theory]
    [InlineData(1, "small.csv")]
    [InlineData(5242880, "boundary.CSV")]
    public async Task AcceptedBoundaryAwaitsMailAndPassesExactBytes(int size, string name)
    {
        using var host = new Host(); using var client = host.Client(); using var form = Upload(size, name);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        host.Mail.BeforeComplete = async token => { entered.SetResult(); await release.Task.WaitAsync(token); };
        Task<HttpResponseMessage> pending = client.PostAsync("/api/gameplay-csv", form);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.False(pending.IsCompleted); release.SetResult();
        using var result = await pending;
        Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
        Assert.Equal(size, host.Mail.Content!.Length); Assert.Equal(1, host.Mail.Calls);
    }

    [Fact]
    public async Task NonFormAndMalformedMultipartNeverSend()
    {
        using var host = new Host(); using var client = host.Client();
        using var result = await client.PostAsync("/api/gameplay-csv", new StringContent("synthetic"));
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        using var broken = new StringContent("not a multipart body");
        broken.Headers.ContentType = new("multipart/form-data");
        using var malformed = await client.PostAsync("/api/gameplay-csv", broken);
        Assert.False(malformed.IsSuccessStatusCode); Assert.Equal(0, host.Mail.Calls);
    }

    [Fact]
    public async Task SmtpFailureCannotReturnSuccess()
    {
        using var host = new Host(); using var client = host.Client(); using var form = Upload(1);
        host.Mail.BeforeComplete = _ => throw new System.Net.Mail.SmtpException("Synthetic delivery failure");
        using var result = await client.PostAsync("/api/gameplay-csv", form);
        Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
        Assert.DoesNotContain("synthetic-not-a-password", await result.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task RequestCancellationReachesSender()
    {
        using var host = new Host(); using var client = host.Client(); using var form = Upload(1);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var canceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        host.Mail.BeforeComplete = async token =>
        {
            using var registration = token.Register(() => canceled.TrySetResult());
            entered.SetResult(); await Task.Delay(Timeout.Infinite, token);
        };
        using var cts = new CancellationTokenSource();
        var request = client.PostAsync("/api/gameplay-csv", form, cts.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10)); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await request);
        await canceled.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task LimiterRejectsTwentyFirstRequestWithoutSending()
    {
        using var host = new Host(); using var client = host.Client();
        for (int i = 0; i < 20; i++)
        {
            using var bad = new StringContent("synthetic");
            using var response = await client.PostAsync("/api/gameplay-csv", bad);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        using var form = Upload(1); using var limited = await client.PostAsync("/api/gameplay-csv", form);
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode); Assert.Equal(0, host.Mail.Calls);
    }

    [Theory]
    [InlineData("Gmail:SmtpPort", "0", "SmtpPort")]
    [InlineData("Gmail:SmtpPort", "65536", "SmtpPort")]
    [InlineData("Gmail:SenderEmail", "invalid", "SenderEmail")]
    [InlineData("Gmail:RecipientEmail", "invalid", "RecipientEmail")]
    [InlineData("Gmail:AppPassword", "", "AppPassword")]
    [InlineData("Gmail:EnableSsl", "false", "Unencrypted SMTP is allowed only on loopback.")]
    public async Task InvalidMailConfigurationFailsAtStartup(string key, string value, string expectedFailure)
    {
        using var original = new Host();
        using var probe = new StartupFailureProbe();
        using var invalid = original.WithWebHostBuilder(builder =>
        {
            builder.UseSetting(key, value);
            builder.ConfigureLogging(logging => logging.AddProvider(probe));
        });
        Assert.ThrowsAny<Exception>(() => invalid.CreateClient());
        var validation = await probe.Failure.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Contains(validation.Failures, failure => failure.Contains(expectedFailure, StringComparison.Ordinal));
        Assert.Equal(0, original.Mail.Calls);
    }
}
