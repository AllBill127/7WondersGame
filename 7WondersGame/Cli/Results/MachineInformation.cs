using System.Runtime;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace _7WondersGame.Cli.Results;

/// <summary>The machine and runtime a run used: timings from different machines or settings don't compare.</summary>
public sealed class MachineInformation
{
    private const string WindowsCpuKey = @"HARDWARE\DESCRIPTION\System\CentralProcessor\0";
    private const string LinuxCpuInformationFile = "/proc/cpuinfo";

    /// <summary>The processor's name.</summary>
    public string Cpu { get; init; } = "";

    /// <summary>How many threads the processor runs at once.</summary>
    public int LogicalCores { get; init; }

    /// <summary>The operating system.</summary>
    public string OperatingSystem { get; init; } = "";

    /// <summary>The .NET runtime.</summary>
    public string Runtime { get; init; } = "";

    /// <summary>"server" or "workstation", and whether it is concurrent.</summary>
    public string GarbageCollector { get; init; } = "";

    /// <summary>This machine.</summary>
    public static MachineInformation Current()
    {
        return new MachineInformation
        {
            Cpu = CpuName(),
            LogicalCores = Environment.ProcessorCount,
            OperatingSystem = RuntimeInformation.OSDescription.Trim(),
            Runtime = RuntimeInformation.FrameworkDescription,
            GarbageCollector = GarbageCollectorDescription(),
        };
    }

    private static string GarbageCollectorDescription()
    {
        string description = "workstation";
        if (GCSettings.IsServerGC)
        {
            description = "server";
        }
        if (GCSettings.LatencyMode != GCLatencyMode.Batch)
        {
            description += ", concurrent";
        }
        return description;
    }

    private static string CpuName()
    {
        string? name = null;
        try
        {
            if (System.OperatingSystem.IsWindows())
            {
                name = WindowsCpuName();
            }
            else if (File.Exists(LinuxCpuInformationFile))
            {
                name = LinuxCpuName();
            }
        }
        catch (Exception)
        {
            // only a label: leave it out rather than fail the run
        }
        if (name != null)
        {
            return name;
        }

        string? identifier = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER");
        if (identifier != null)
        {
            return identifier;
        }
        return "unknown";
    }

    [SupportedOSPlatform("windows")]
    private static string? WindowsCpuName()
    {
        using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(WindowsCpuKey))
        {
            if (key != null && key.GetValue("ProcessorNameString") is string name)
            {
                return name.Trim();
            }
        }
        return null;
    }

    private static string? LinuxCpuName()
    {
        string? line = File.ReadLines(LinuxCpuInformationFile)
            .FirstOrDefault(cpuInformationLine => cpuInformationLine.StartsWith("model name"));
        if (line == null)
        {
            return null;
        }
        int nameStart = line.IndexOf(':') + 1;
        return line.Substring(nameStart).Trim();
    }
}
