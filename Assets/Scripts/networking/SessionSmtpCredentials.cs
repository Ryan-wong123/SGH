using System;
using System.Net.Mail;

public static class SessionSmtpCredentials
{
    public const string OrganizerActorNumberRoomPropertyKey = "SessionOrganizerActorNumber";

    private static string senderEmail;
    private static string appPassword;
    private static string recipientEmail;

    public static bool TrySetSenderEmail(string value, out string error)
    {
        if (!TryNormalizeEmail(value, out senderEmail))
        {
            error = "Enter a valid sender email.";
            return false;
        }

        error = null;
        return true;
    }

    public static bool TrySetAppPassword(string value, out string error)
    {
        appPassword = value?.Replace(" ", string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(appPassword))
        {
            error = "Gmail app password is required.";
            return false;
        }

        error = null;
        return true;
    }

    public static bool TrySetRecipientEmail(string value, out string error)
    {
        if (!TryNormalizeEmail(value, out recipientEmail))
        {
            error = "Enter a valid recipient email.";
            return false;
        }

        error = null;
        return true;
    }

    public static bool TryGetCredentials(out string sender, out string password, out string recipient)
    {
        sender = senderEmail;
        password = appPassword;
        recipient = recipientEmail;
        return !string.IsNullOrWhiteSpace(sender) && !string.IsNullOrWhiteSpace(password) && !string.IsNullOrWhiteSpace(recipient);
    }

    public static void Clear()
    {
        senderEmail = null;
        appPassword = null;
        recipientEmail = null;
    }

    private static bool TryNormalizeEmail(string value, out string normalized)
    {
        normalized = null;
        string trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return false;

        try
        {
            var parsed = new MailAddress(trimmed);
            if (!string.Equals(parsed.Address, trimmed, StringComparison.OrdinalIgnoreCase))
                return false;

            normalized = trimmed;
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
