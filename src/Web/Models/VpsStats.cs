namespace Web.Models;

public class VpsStats
{
    public CpuInfo Cpu { get; set; } = new();
    public MemoryInfo Memory { get; set; } = new();
    public DiskInfo Disk { get; set; } = new();
    public UptimeInfo Uptime { get; set; } = new();
    public LoadAverageInfo LoadAverage { get; set; } = new();
    public string Hostname { get; set; } = "";
    public ContainerInfo[] Containers { get; set; } = Array.Empty<ContainerInfo>();
    public ProcessInfo[] Processes { get; set; } = Array.Empty<ProcessInfo>();
}

public class CpuInfo
{
    public double UsagePercent { get; set; }
    public int Cores { get; set; }
    public string Model { get; set; } = "";
}

public class MemoryInfo
{
    public double TotalMb { get; set; }
    public double UsedMb { get; set; }
    public double AvailableMb { get; set; }
    public double UsagePercent { get; set; }
}

public class DiskInfo
{
    public string Total { get; set; } = "";
    public string Used { get; set; } = "";
    public string Available { get; set; } = "";
    public string UsagePercent { get; set; } = "0";
}

public class UptimeInfo
{
    public int Days { get; set; }
    public int Hours { get; set; }
    public int Minutes { get; set; }
    public long TotalSeconds { get; set; }
    public string Formatted { get; set; } = "";
}

public class LoadAverageInfo
{
    public string Load1 { get; set; } = "0";
    public string Load5 { get; set; } = "0";
    public string Load15 { get; set; } = "0";
}

public class ContainerInfo
{
    public string Name { get; set; } = "";
    public string Image { get; set; } = "";
    public string State { get; set; } = "";
    public string Status { get; set; } = "";
    public string Created { get; set; } = "";
}

public class ProcessInfo
{
    public string User { get; set; } = "";
    public string Pid { get; set; } = "";
    public string Cpu { get; set; } = "";
    public string Mem { get; set; } = "";
    public string Command { get; set; } = "";
}
