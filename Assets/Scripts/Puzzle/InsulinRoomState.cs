using ExitGames.Client.Photon;

// One master-written snapshot, updated together in a room-property operation.
// Revision changes on reset; requests and deadline completion carry that revision.
public static class InsulinRoomState
{
    public const string RevisionKey = "SGH_Insulin_Revision";
    public const string PartKey = "SGH_Insulin_Part";
    public const string AdvanceAtKey = "SGH_Insulin_AdvanceAt";

    public static bool TryRead(Hashtable properties, out int revision, out int part, out double advanceAt)
    {
        revision = 0; part = 1; advanceAt = 0d;
        if (properties == null ||
            !properties.TryGetValue(RevisionKey, out object revisionValue) || revisionValue is not int r || r < 0 ||
            !properties.TryGetValue(PartKey, out object partValue) || partValue is not int p || (p != 1 && p != 2) ||
            !properties.TryGetValue(AdvanceAtKey, out object dueValue) || dueValue is not double due ||
            double.IsNaN(due) || double.IsInfinity(due) || due < 0d || (p == 2 && due != 0d))
            return false;
        revision = r; part = p; advanceAt = due;
        return true;
    }

    public static Hashtable Snapshot(int revision, int part, double advanceAt)
    {
        return new Hashtable { [RevisionKey] = revision, [PartKey] = part, [AdvanceAtKey] = advanceAt };
    }

    public static bool CanSchedule(Hashtable properties, int requestRevision)
    {
        return TryRead(properties, out int revision, out int part, out double due) &&
               revision == requestRevision && part == 1 && due == 0d;
    }

    public static bool IsDue(Hashtable properties, double now)
    {
        return !double.IsNaN(now) && !double.IsInfinity(now) &&
               TryRead(properties, out _, out int part, out double due) && part == 1 && due > 0d && now >= due;
    }
}
