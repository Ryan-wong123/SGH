using System.ComponentModel.DataAnnotations;

namespace SghCsvEmailApi.Options;

public sealed class GmailOptions
{
    public const string SectionName = "Gmail";

    [Required]
    public string SmtpHost { get; init; } = "smtp.gmail.com";

    [Range(1, 65535)]
    public int SmtpPort { get; init; } = 587;
}
