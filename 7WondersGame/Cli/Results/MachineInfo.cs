namespace _7WondersGame.Cli.Results;

/// <summary>The machine and runtime a run used (step 8): timings from different machines or settings don't compare.</summary>
public sealed class MachineInfo
{
    public string Cpu { get; init; } = "";
    public int LogicalCores { get; init; }
    public string Os { get; init; } = "";
    public string Runtime { get; init; } = "";
    /// <summary>"server" or "workstation", and whether it is concurrent.</summary>
    public string Gc { get; init; } = "";

    public static MachineInfo Current() => new()
    {
        Cpu = CpuName(),
        LogicalCores = Environment.ProcessorCount,
        Os = System.Runtime.InteropServices.RuntimeInformation.OSDescription.Trim(),
        Runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
        Gc = (System.Runtime.GCSettings.IsServerGC ? "server" : "workstation") +
             (System.Runtime.GCSettings.LatencyMode == System.Runtime.GCLatencyMode.Batch ? "" : ", concurrent"),
    };

    private static string CpuName()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                if (key?.GetValue("ProcessorNameString") is string name)
                    return name.Trim();
            }
            else if (File.Exists("/proc/cpuinfo"))
            {
                string? line = File.ReadLines("/proc/cpuinfo").FirstOrDefault(l => l.StartsWith("model name"));
                if (line != null)
                    return line[(line.IndexOf(':') + 1)..].Trim();
            }
        }
        catch (Exception)
        {
            // only a label: leave it out rather than fail the run
        }
        return Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "unknown";
    }
}
