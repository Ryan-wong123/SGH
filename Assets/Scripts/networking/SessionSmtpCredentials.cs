public static class SessionSmtpCredentials
{
    public const string OrganizerActorNumberRoomPropertyKey = "SessionOrganizerActorNumber";

    public static void Clear()
    {
        // Email settings now live only in the backend, so there is no local email state to clear.
    }
}
