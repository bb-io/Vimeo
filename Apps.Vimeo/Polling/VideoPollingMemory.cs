namespace Apps.Vimeo.Polling;

public class VideoPollingMemory
{
    public DateTime StartedAt { get; set; }

    public DateTime ScannedThrough { get; set; }

    public Dictionary<string, DateTime> EmittedVideoIds { get; set; } = new(StringComparer.Ordinal);
}
