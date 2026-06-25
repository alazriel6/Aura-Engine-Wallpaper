using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using LibreHardwareMonitor.Hardware;
using LiveWallpaperApp.Models;

namespace LiveWallpaperApp.Services;

public sealed class PerformanceService : IDisposable
{
    private readonly DispatcherTimer _timer;
    private readonly Process _process = Process.GetCurrentProcess();
    private Computer? _computer;
    private DateTimeOffset _lastProcessSample = DateTimeOffset.Now;
    private TimeSpan _lastProcessorTime;
    private bool _disposed;

    public PerformanceService()
    {
        _lastProcessorTime = _process.TotalProcessorTime;

        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _timer.Tick += (_, _) => PublishSnapshot();
    }

    public event EventHandler<SystemPerformanceSnapshot>? SnapshotUpdated;

    public SystemPerformanceSnapshot Current { get; private set; } = new();

    public string CpuName 
    {
        get 
        {
            var rawName = _computer?.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu)?.Name ?? Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "Unknown CPU";
            int wIndex = rawName.IndexOf(" w/ ", StringComparison.OrdinalIgnoreCase);
            if (wIndex > 0) rawName = rawName.Substring(0, wIndex);
            return rawName;
        }
    }

    public string GpuName 
    {
        get 
        {
            if (_computer is null) return "Unknown GPU";
            var gpus = _computer.Hardware
                .Where(h => h.HardwareType is HardwareType.GpuAmd or HardwareType.GpuIntel or HardwareType.GpuNvidia)
                .Select(h => h.Name)
                .OrderByDescending(g => g.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) || g.Contains("RTX", StringComparison.OrdinalIgnoreCase) || g.Contains("RX ", StringComparison.OrdinalIgnoreCase))
                .ToList();
            
            return gpus.Count > 0 ? string.Join(" + ", gpus) : "Unknown GPU";
        }
    }

    public string TotalRam => $"{Math.Round(QueryMemory().TotalPhysical / 1024.0 / 1024.0 / 1024.0)} GB";

    public void Start(bool detailedHardwareSensors = false)
    {
        if (detailedHardwareSensors)
        {
            EnableDetailedHardwareSensors();
        }

        _timer.Start();
        PublishSnapshot();
    }

    public void EnableDetailedHardwareSensors()
    {
        if (_computer is not null)
        {
            return;
        }

        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = false,
            IsControllerEnabled = false,
            IsMotherboardEnabled = false,
            IsNetworkEnabled = false,
            IsStorageEnabled = false
        };

        try
        {
            _computer.Open();
        }
        catch
        {
            _computer = null;
        }
    }

    public void Stop()
    {
        _timer.Stop();
    }

    private void PublishSnapshot()
    {
        if (_disposed)
        {
            return;
        }

        UpdateHardware();

        var now = DateTimeOffset.Now;
        _process.Refresh();

        var processorTime = _process.TotalProcessorTime;
        var elapsedMs = Math.Max(1, (now - _lastProcessSample).TotalMilliseconds);
        var cpuDeltaMs = Math.Max(0, (processorTime - _lastProcessorTime).TotalMilliseconds);
        var appCpu = Math.Clamp(cpuDeltaMs / (elapsedMs * Environment.ProcessorCount) * 100.0, 0, 100);

        _lastProcessSample = now;
        _lastProcessorTime = processorTime;

        var memory = QueryMemory();
        var sensorSnapshot = ReadSensors();

        Current = new SystemPerformanceSnapshot
        {
            Timestamp = now,
            CpuUsagePercent = sensorSnapshot.CpuLoadPercent > 0 ? sensorSnapshot.CpuLoadPercent : appCpu,
            AppCpuUsagePercent = appCpu,
            GpuUsagePercent = sensorSnapshot.GpuLoadPercent,
            RamUsagePercent = memory.MemoryLoadPercent,
            AppRamMb = _process.WorkingSet64 / 1024.0 / 1024.0,
            VramUsageMb = sensorSnapshot.VramUsedMb,
            VramTotalMb = sensorSnapshot.VramTotalMb,
            CpuTemperatureCelsius = sensorSnapshot.CpuTemperatureCelsius,
            DecoderUsagePercent = sensorSnapshot.GpuVideoDecodePercent,
            WallpaperFps = 0,
            RenderLatencyMs = 0,
            FrameDrops = 0
        };

        SnapshotUpdated?.Invoke(this, Current);
    }

    private void UpdateHardware()
    {
        if (_computer is null)
        {
            return;
        }

        foreach (var hardware in _computer.Hardware)
        {
            hardware.Update();
            foreach (var subHardware in hardware.SubHardware)
            {
                subHardware.Update();
            }
        }
    }

    private SensorSnapshot ReadSensors()
    {
        var snapshot = new SensorSnapshot();

        if (_computer is null)
        {
            return snapshot;
        }

        foreach (var hardware in _computer.Hardware.SelectMany(FlattenHardware))
        {
            foreach (var sensor in hardware.Sensors)
            {
                var value = sensor.Value.GetValueOrDefault();
                if (value <= 0)
                {
                    continue;
                }

                if (sensor.SensorType == SensorType.Load && sensor.Name.Contains("CPU Total", StringComparison.OrdinalIgnoreCase))
                {
                    snapshot.CpuLoadPercent = Math.Max(snapshot.CpuLoadPercent, value);
                }

                if (sensor.SensorType == SensorType.Load
                    && (hardware.HardwareType is HardwareType.GpuAmd or HardwareType.GpuIntel or HardwareType.GpuNvidia)
                    && sensor.Name.Contains("Core", StringComparison.OrdinalIgnoreCase))
                {
                    snapshot.GpuLoadPercent = Math.Max(snapshot.GpuLoadPercent, value);
                }

                if (sensor.SensorType == SensorType.Load
                    && sensor.Name.Contains("Video", StringComparison.OrdinalIgnoreCase))
                {
                    snapshot.GpuVideoDecodePercent = Math.Max(snapshot.GpuVideoDecodePercent, value);
                }

                if (sensor.SensorType == SensorType.Temperature
                    && hardware.HardwareType == HardwareType.Cpu
                    && (sensor.Name.Contains("Package", StringComparison.OrdinalIgnoreCase)
                        || sensor.Name.Contains("CPU", StringComparison.OrdinalIgnoreCase)))
                {
                    snapshot.CpuTemperatureCelsius = Math.Max(snapshot.CpuTemperatureCelsius, value);
                }

                if (sensor.SensorType == SensorType.SmallData
                    && sensor.Name.Contains("GPU Memory Used", StringComparison.OrdinalIgnoreCase))
                {
                    snapshot.VramUsedMb = Math.Max(snapshot.VramUsedMb, value);
                }

                if (sensor.SensorType == SensorType.SmallData
                    && sensor.Name.Contains("GPU Memory Total", StringComparison.OrdinalIgnoreCase))
                {
                    snapshot.VramTotalMb = Math.Max(snapshot.VramTotalMb, value);
                }
            }
        }

        return snapshot;
    }

    private static IEnumerable<IHardware> FlattenHardware(IHardware hardware)
    {
        yield return hardware;

        foreach (var child in hardware.SubHardware)
        {
            foreach (var item in FlattenHardware(child))
            {
                yield return item;
            }
        }
    }

    private static MemoryStatus QueryMemory()
    {
        var status = new MemoryStatus
        {
            Length = (uint)Marshal.SizeOf<MemoryStatus>()
        };

        return GlobalMemoryStatusEx(ref status) ? status : new MemoryStatus();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _timer.Stop();
        _computer?.Close();
    }

    private sealed class SensorSnapshot
    {
        public double CpuLoadPercent { get; set; }
        public double GpuLoadPercent { get; set; }
        public double GpuVideoDecodePercent { get; set; }
        public double VramUsedMb { get; set; }
        public double VramTotalMb { get; set; }
        public double CpuTemperatureCelsius { get; set; }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatus
    {
        public uint Length;
        public uint MemoryLoadPercent;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatus lpBuffer);
}
