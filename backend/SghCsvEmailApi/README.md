# SGH CSV email API

Requires .NET 10. Configure Gmail__SenderEmail, Gmail__AppPassword and
Gmail__RecipientEmail using user secrets or deployment environment variables.
Gmail__SmtpHost and Gmail__SmtpPort configure the server. Never put credentials
in source. SMTP TLS defaults on; disabling it is permitted only on loopback for
an isolated local test server. This service is separate from Unity direct SMTP.

GET /health returns 200. POST /api/gameplay-csv accepts multipart file `csv`,
roomName and playedAt. A nonempty .csv file up to 5 MiB is required. The endpoint
awaits mail delivery before 204, propagates request cancellation, and limits the
shared endpoint to 20 requests/minute. HTTPS redirection remains enabled.

Run isolated HTTP and local SMTP tests (no real mail or personal CSVs):

    dotnet test backend/SghCsvEmailApi.Tests/SghCsvEmailApi.Tests.csproj

Integration tests replace mail delivery in HTTP fixtures. Separate transport
fixtures send only to an ephemeral loopback SMTP listener with synthetic .invalid
addresses and exercise MIME attachments, rejection and cancellation. No external
mailbox or production SMTP credentials are used.
