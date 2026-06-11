namespace LiveWallpaperApp.Models;

public sealed class SystemPerformanceSnapshot
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;
    public double CpuUsagePercent { get; init; }
    public double AppCpuUsagePercent { get; init; }
    public double GpuUsagePercent { get; init; }
    public double RamUsagePercent { get; init; }
    public double AppRamMb { get; init; }
    public double VramUsageMb { get; init; }
    public double VramTotalMb { get; init; }
    public double CpuTemperatureCelsius { get; init; }
    public double DecoderUsagePercent { get; init; }
    public double WallpaperFps { get; init; }
    public double RenderLatencyMs { get; init; }
    public int FrameDrops { get; init; }

    public string CpuText => $"{CpuUsagePercent:0}% CPU";
    public string GpuText => $"{GpuUsagePercent:0}% GPU";
    public string RamText => $"{RamUsagePercent:0}% RAM";
    public string VramText => VramTotalMb > 0 ? $"{VramUsageMb:0}/{VramTotalMb:0} MB VRAM" : "VRAM n/a";
    public string TemperatureText => CpuTemperatureCelsius > 0 ? $"{CpuTemperatureCelsius:0} C" : "Temp n/a";
}
