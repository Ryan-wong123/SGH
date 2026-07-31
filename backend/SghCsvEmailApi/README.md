# SGH CSV Email API

This backend sends completed-game CSVs for the Unity client. The Gmail app password stays on this server; it is never included in Unity.

## Local development setup

Put the app password in the `GmailAppPassword` constant in `Services/CsvEmailSender.cs`, then run:

```powershell
dotnet run
```

Set `GmailSenderEmail`, `GmailAppPassword`, and `RecipientEmail` in `Services/CsvEmailSender.cs`. The game does not ask players for either email address.

Deploy the API behind HTTPS. Then set `emailApiUrl` in `Assets/Resources/SmtpEmailSettings.asset` to `https://your-domain/api/gameplay-csv`.

For an Editor-only local test on the same computer, run `dotnet run` and use `http://localhost:5177/api/gameplay-csv`. Standalone builds still require an HTTPS backend URL.

## Security note

The backend keeps Gmail credentials private, but the endpoint accepts a recipient supplied by the game. Before publishing publicly, protect this endpoint with real organiser authentication (rather than a key embedded in Unity) and configure production rate limits/firewall rules appropriate for your deployment.
