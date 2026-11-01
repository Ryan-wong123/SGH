using ExitGames.Client.Photon;

// Application protocol inventory. Codes 41-43 are retired: older builds reused
// them across incompatible payloads. Network partitions these builds by version.
public static class SghPhotonEvents
{
    public const string ProtocolVersion = "sgh2";
    public const byte Analytics = 61;
    public const byte ClueCloseRemaining = 62;
    public const byte CrosswordText = 63;
    public const byte CrosswordResult = 64;
    public const byte WordSearchState = 65;

    public static bool TryGetPayload(EventData message, byte code, int length, out object[] payload)
    {
        payload = null;
        if (message == null || message.Code != code ||
            message.CustomData is not object[] values || values.Length != length)
            return false;
        payload = values;
        return true;
    }
}
