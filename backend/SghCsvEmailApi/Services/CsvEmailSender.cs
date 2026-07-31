using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using SghCsvEmailApi.Options;

namespace SghCsvEmailApi.Services;

public sealed class CsvEmailSender
{
    // Set the Gmail account that generated the app password below.
    private const string GmailSenderEmail = "wong.ryan5091@gmail.com";

    // Paste the 16-character Gmail App Password here. This file must remain on the backend server only.
    private const string GmailAppPassword = "snwi iaxa qxbl pixd";

    private readonly GmailOptions settings;
    private readonly ILogger<CsvEmailSender> logger;

    public CsvEmailSender(IOptions<GmailOptions> settings, ILogger<CsvEmailSender> logger)
    {
        this.settings = settings.Value;
        this.logger = logger;
    }

    // Set the fixed address that receives every gameplay CSV.
    private const string RecipientEmail = "wong.ryan5091@gmail.com";

    public async Task SendAsync(IFormFile csv, string roomName, string playedAt)
    {
        await using Stream csvStream = csv.OpenReadStream();
        using var message = new MailMessage
        {
            From = new MailAddress(GmailSenderEmail),
            Subject = $"Gameplay CSV - {roomName}",
            Body = $"Gameplay session for room '{roomName}' completed at {playedAt}. The CSV is attached."
        };
        message.To.Add(RecipientEmail);
        message.Attachments.Add(new Attachment(csvStream, Path.GetFileName(csv.FileName), "text/csv"));

        using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
        {
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(GmailSenderEmail, GmailAppPassword.Replace(" ", string.Empty))
        };

        await client.SendMailAsync(message);
        logger.LogInformation("Sent gameplay CSV for room {RoomName} to {RecipientEmail}.", roomName, RecipientEmail);
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
