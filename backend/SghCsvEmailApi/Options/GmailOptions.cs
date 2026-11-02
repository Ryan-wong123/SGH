using System.ComponentModel.DataAnnotations;

namespace SghCsvEmailApi.Options;

public sealed class GmailOptions
{
    public const string SectionName = "Gmail";

    [Required]
    public string SmtpHost { get; init; } = "smtp.gmail.com";

    [Required, EmailAddress]
    public string SenderEmail { get; init; } = "";

    [Required]
    public string AppPassword { get; init; } = "";

    [Required, EmailAddress]
    public string RecipientEmail { get; init; } = "";

    public bool EnableSsl { get; init; } = true;

    [Range(1, 65535)]
    public int SmtpPort { get; init; } = 587;
}
