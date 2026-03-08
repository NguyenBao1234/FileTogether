namespace FileTogether.Core;

public class TransferProgress
{
    public long TotalBytes { get; set; }
    public long TransferredBytes { get; set; }
    public int Percentage { get; set; }
    public double SpeedBytesPerSecond { get; set; }
    public TimeSpan EstimatedTimeRemaining { get; set; } //Estimated Time of Arrival
    
    public string GetFormattedSpeed()
    {
        if (SpeedBytesPerSecond < 1024)
            return $"{SpeedBytesPerSecond:F0} B/s";
        if (SpeedBytesPerSecond < 1024 * 1024)
            return $"{SpeedBytesPerSecond / 1024:F2} KB/s";
        if (SpeedBytesPerSecond < 1024 * 1024 * 1024)
            return $"{SpeedBytesPerSecond / (1024 * 1024):F2} MB/s";
        return $"{SpeedBytesPerSecond / (1024 * 1024 * 1024):F2} GB/s";
    }
        
    public string GetFormattedETA() //Estimated Time of Arrival
    {
        if (EstimatedTimeRemaining.TotalSeconds < 1)
            return "< 1s";
        if (EstimatedTimeRemaining.TotalMinutes < 1)
            return $"{EstimatedTimeRemaining.Seconds}s";
        if (EstimatedTimeRemaining.TotalHours < 1)
            return $"{EstimatedTimeRemaining.Minutes}m {EstimatedTimeRemaining.Seconds}s";
        return $"{(int)EstimatedTimeRemaining.TotalHours}h {EstimatedTimeRemaining.Minutes}m";
    }
}