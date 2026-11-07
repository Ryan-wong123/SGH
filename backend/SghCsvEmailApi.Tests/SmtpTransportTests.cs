using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SghCsvEmailApi.Options;
using SghCsvEmailApi.Services;
using Xunit;

namespace SghCsvEmailApi.Tests;

public class SmtpTransportTests
{
    // A real TCP SMTP conversation bound to loopback only. It cannot forward mail.
    private sealed class Inbox : IAsyncDisposable
    {
        private readonly TcpListener listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource stop = new(TimeSpan.FromSeconds(15));
        private readonly Task worker;
        public readonly TaskCompletionSource<string> Message = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource DataStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Port => ((IPEndPoint)listener.LocalEndpoint).Port;
        public Inbox(bool reject = false, bool stall = false)
        {
            listener.Start(); worker = Run(reject, stall);
        }
        private async Task Run(bool reject, bool stall)
        {
            try
            {
                using var connection = await listener.AcceptTcpClientAsync(stop.Token);
                using var stream = connection.GetStream();
                using var reader = new StreamReader(stream, Encoding.ASCII);
                using var writer = new StreamWriter(stream, Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true };
                await writer.WriteLineAsync("220 synthetic ESMTP");
                while (!stop.IsCancellationRequested)
                {
                    string? line = await reader.ReadLineAsync(stop.Token);
                    if (line == null) return;
                    if (line.StartsWith("EHLO") || line.StartsWith("HELO")) await writer.WriteLineAsync("250 synthetic");
                    else if (line.StartsWith("MAIL FROM:")) await writer.WriteLineAsync("250 OK");
                    else if (line.StartsWith("RCPT TO:")) await writer.WriteLineAsync(reject ? "550 synthetic recipient rejected" : "250 OK");
                    else if (line == "DATA")
                    {
                        DataStarted.TrySetResult();
                        if (stall) { await Task.Delay(Timeout.Infinite, stop.Token); return; }
                        await writer.WriteLineAsync("354 send synthetic message");
                        var body = new StringBuilder();
                        while ((line = await reader.ReadLineAsync(stop.Token)) != null && line != ".") body.AppendLine(line);
                        Message.TrySetResult(body.ToString()); await writer.WriteLineAsync("250 queued locally");
                    }
                    else if (line == "QUIT") { await writer.WriteLineAsync("221 bye"); return; }
                    else await writer.WriteLineAsync("250 OK");
                }
            }
            catch (Exception e) when (e is OperationCanceledException or IOException or ObjectDisposedException) { }
        }
        public async ValueTask DisposeAsync()
        {
            stop.Cancel(); listener.Stop(); await worker; stop.Dispose();
        }
    }

    private static CsvEmailSender Sender(Inbox inbox) => new(Microsoft.Extensions.Options.Options.Create(new GmailOptions
    {
        SmtpHost = "127.0.0.1", SmtpPort = inbox.Port, EnableSsl = false,
        SenderEmail = "sender@example.invalid", RecipientEmail = "recipient@example.invalid",
        AppPassword = "synthetic-only"
    }), NullLogger<CsvEmailSender>.Instance);

    [Fact]
    public async Task DeliversSyntheticMimeAttachmentToLoopbackInbox()
    {
        await using var inbox = new Inbox();
        using var bytes = new MemoryStream(Encoding.UTF8.GetBytes("synthetic,value\nA,1\n"));
        var file = new FormFile(bytes, 0, bytes.Length, "csv", "../synthetic.csv");
        await Sender(inbox).SendAsync(file, "synthetic-room", "synthetic-date");
        string mime = await inbox.Message.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Contains("synthetic.csv", mime); Assert.Contains("text/csv", mime);
        Assert.Contains(Convert.ToBase64String(Encoding.UTF8.GetBytes("synthetic,value\nA,1\n")), mime);
        Assert.DoesNotContain("../synthetic.csv", mime);
        Assert.Contains("sender@example.invalid", mime); Assert.Contains("recipient@example.invalid", mime);
        Assert.DoesNotContain("synthetic-only", mime);
    }

    [Fact]
    public async Task RejectedRecipientFailsDelivery()
    {
        await using var inbox = new Inbox(reject: true);
        using var bytes = new MemoryStream(new byte[] { 1 });
        await Assert.ThrowsAnyAsync<SmtpException>(() => Sender(inbox).SendAsync(
            new FormFile(bytes, 0, 1, "csv", "synthetic.csv"), "synthetic-room", "synthetic-date"));
        Assert.False(inbox.Message.Task.IsCompleted);
    }

    [Fact]
    public async Task CancellationInterruptsStalledSmtpInsteadOfClaimingDelivery()
    {
        await using var inbox = new Inbox(stall: true);
        using var bytes = new MemoryStream(new byte[] { 1 }); using var cts = new CancellationTokenSource();
        Task send = Sender(inbox).SendAsync(new FormFile(bytes, 0, 1, "csv", "synthetic.csv"), "synthetic-room", "synthetic-date", cts.Token);
        await inbox.DataStarted.Task.WaitAsync(TimeSpan.FromSeconds(5)); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => send);
        Assert.False(inbox.Message.Task.IsCompleted);
    }
}
