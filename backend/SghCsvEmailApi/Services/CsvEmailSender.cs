using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using SghCsvEmailApi.Options;

namespace SghCsvEmailApi.Services;

public sealed class CsvEmailSender : ICsvEmailSender
{
    private readonly GmailOptions settings;
    private readonly ILogger<CsvEmailSender> logger;

    public CsvEmailSender(IOptions<GmailOptions> settings, ILogger<CsvEmailSender> logger)
    {
        this.settings = settings.Value;
        this.logger = logger;
    }

    public async Task SendAsync(IFormFile csv, string roomName, string playedAt, CancellationToken cancellationToken = default)
    {
        await using Stream csvStream = csv.OpenReadStream();
        using var message = new MailMessage
        {
            From = new MailAddress(settings.SenderEmail),
            Subject = $"Gameplay CSV - {roomName}",
            Body = $"Gameplay session for room '{roomName}' completed at {playedAt}. The CSV is attached."
        };
        message.To.Add(settings.RecipientEmail);
        message.Attachments.Add(new Attachment(csvStream, Path.GetFileName(csv.FileName), "text/csv"));

        using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
        {
            EnableSsl = settings.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(settings.SenderEmail, settings.AppPassword.Replace(" ", string.Empty))
        };

        await client.SendMailAsync(message, cancellationToken);
        logger.LogInformation("Gameplay CSV delivery completed.");
    }

    public static bool IsValidEmail(string email)
    {
        try
        {
            var parsed = new MailAddress(email.Trim());
            return string.Equals(parsed.Address, email.Trim(), StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
