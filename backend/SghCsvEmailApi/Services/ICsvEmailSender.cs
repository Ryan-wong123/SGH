namespace SghCsvEmailApi.Services;

public interface ICsvEmailSender
{
    Task SendAsync(IFormFile csv, string roomName, string playedAt, CancellationToken cancellationToken = default);
}
