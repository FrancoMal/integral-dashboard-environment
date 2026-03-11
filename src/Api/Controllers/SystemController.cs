using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SystemController : ControllerBase
{
    /// <summary>
    /// Returns real VPS system metrics read from /proc and Docker socket.
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var stats = new
        {
            cpu = GetCpuInfo(),
            memory = GetMemoryInfo(),
            disk = GetDiskInfo(),
            uptime = GetUptime(),
            loadAverage = GetLoadAverage(),
            hostname = GetHostname(),
            containers = await GetDockerContainersAsync(),
            processes = GetTopProcesses()
        };

        return Ok(stats);
    }

    // ── CPU ──────────────────────────────────────────────
    private object GetCpuInfo()
    {
        try
        {
            // Read /proc/stat for usage calculation
            var statLines = System.IO.File.ReadAllLines("/host_proc/stat");
            var cpuLine = statLines.FirstOrDefault(l => l.StartsWith("cpu "));
            double usagePercent = 0;

            if (cpuLine != null)
            {
                var parts = cpuLine.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1)
                    .Select(p => double.TryParse(p, out var v) ? v : 0).ToArray();
                if (parts.Length >= 4)
                {
                    var idle = parts[3];
                    var total = parts.Sum();
                    usagePercent = total > 0 ? Math.Round((1 - idle / total) * 100, 1) : 0;
                }
            }

            // Count cores
            int cores = 0;
            string model = "Desconocido";
            if (System.IO.File.Exists("/host_proc/cpuinfo"))
            {
                var lines = System.IO.File.ReadAllLines("/host_proc/cpuinfo");
                cores = lines.Count(l => l.StartsWith("processor"));
                var modelLine = lines.FirstOrDefault(l => l.StartsWith("model name"));
                if (modelLine != null)
                    model = modelLine.Split(':').Last().Trim();
            }

            return new { usagePercent, cores, model };
        }
        catch
        {
            return new { usagePercent = 0.0, cores = 0, model = "No disponible" };
        }
    }

    // ── Memory ───────────────────────────────────────────
    private object GetMemoryInfo()
    {
        try
        {
            var lines = System.IO.File.ReadAllLines("/host_proc/meminfo");
            long Get(string key)
            {
                var line = lines.FirstOrDefault(l => l.StartsWith(key + ":"));
                if (line == null) return 0;
                var val = line.Split(':').Last().Trim().Split(' ').First();
                return long.TryParse(val, out var v) ? v : 0;
            }

            var totalKb = Get("MemTotal");
            var availableKb = Get("MemAvailable");
            var usedKb = totalKb - availableKb;
            var usagePercent = totalKb > 0 ? Math.Round((double)usedKb / totalKb * 100, 1) : 0;

            return new
            {
                totalMb = Math.Round(totalKb / 1024.0),
                usedMb = Math.Round(usedKb / 1024.0),
                availableMb = Math.Round(availableKb / 1024.0),
                usagePercent
            };
        }
        catch
        {
            return new { totalMb = 0.0, usedMb = 0.0, availableMb = 0.0, usagePercent = 0.0 };
        }
    }

    // ── Disk ─────────────────────────────────────────────
    private object GetDiskInfo()
    {
        try
        {
            var psi = new ProcessStartInfo("df", "-h /")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            var output = proc?.StandardOutput.ReadToEnd() ?? "";
            proc?.WaitForExit();

            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length >= 2)
            {
                var parts = lines[1].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 5)
                {
                    return new
                    {
                        total = parts[1],
                        used = parts[2],
                        available = parts[3],
                        usagePercent = parts[4].TrimEnd('%')
                    };
                }
            }
            return new { total = "N/A", used = "N/A", available = "N/A", usagePercent = "0" };
        }
        catch
        {
            return new { total = "N/A", used = "N/A", available = "N/A", usagePercent = "0" };
        }
    }

    // ── Uptime ───────────────────────────────────────────
    private object GetUptime()
    {
        try
        {
            var raw = System.IO.File.ReadAllText("/host_proc/uptime").Trim();
            var seconds = double.Parse(raw.Split(' ')[0], CultureInfo.InvariantCulture);
            var ts = TimeSpan.FromSeconds(seconds);
            return new
            {
                days = ts.Days,
                hours = ts.Hours,
                minutes = ts.Minutes,
                totalSeconds = (long)seconds,
                formatted = $"{ts.Days}d {ts.Hours}h {ts.Minutes}m"
            };
        }
        catch
        {
            return new { days = 0, hours = 0, minutes = 0, totalSeconds = 0L, formatted = "N/A" };
        }
    }

    // ── Load Average ─────────────────────────────────────
    private object GetLoadAverage()
    {
        try
        {
            var raw = System.IO.File.ReadAllText("/host_proc/loadavg").Trim();
            var parts = raw.Split(' ');
            return new
            {
                load1 = parts.ElementAtOrDefault(0) ?? "0",
                load5 = parts.ElementAtOrDefault(1) ?? "0",
                load15 = parts.ElementAtOrDefault(2) ?? "0"
            };
        }
        catch
        {
            return new { load1 = "0", load5 = "0", load15 = "0" };
        }
    }

    // ── Hostname ─────────────────────────────────────────
    private string GetHostname()
    {
        try
        {
            if (System.IO.File.Exists("/host_proc/sys/kernel/hostname"))
                return System.IO.File.ReadAllText("/host_proc/sys/kernel/hostname").Trim();
            return Environment.MachineName;
        }
        catch
        {
            return Environment.MachineName;
        }
    }

    // ── Docker Containers ────────────────────────────────
    private async Task<object[]> GetDockerContainersAsync()
    {
        try
        {
            // Connect to Docker socket via Unix domain socket
            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            var endpoint = new UnixDomainSocketEndPoint("/var/run/docker.sock");
            await socket.ConnectAsync(endpoint);

            var request = "GET /containers/json?all=true HTTP/1.0\r\nHost: localhost\r\n\r\n";
            await socket.SendAsync(Encoding.ASCII.GetBytes(request));

            var buffer = new byte[65536];
            var sb = new StringBuilder();
            int read;
            while ((read = await socket.ReceiveAsync(buffer)) > 0)
                sb.Append(Encoding.UTF8.GetString(buffer, 0, read));
            socket.Close();

            var response = sb.ToString();
            var bodyStart = response.IndexOf("\r\n\r\n");
            if (bodyStart < 0) return Array.Empty<object>();
            var json = response[(bodyStart + 4)..];

            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.EnumerateArray().Select(c =>
            {
                var names = c.GetProperty("Names").EnumerateArray()
                    .Select(n => n.GetString()?.TrimStart('/')).ToArray();
                var state = c.GetProperty("State").GetString() ?? "unknown";
                var status = c.GetProperty("Status").GetString() ?? "";
                var image = c.GetProperty("Image").GetString() ?? "";
                var created = c.GetProperty("Created").GetInt64();
                var createdDt = DateTimeOffset.FromUnixTimeSeconds(created).LocalDateTime;

                return (object)new
                {
                    name = names.FirstOrDefault() ?? "sin-nombre",
                    image,
                    state,
                    status,
                    created = createdDt.ToString("yyyy-MM-dd HH:mm")
                };
            }).ToArray();
        }
        catch
        {
            return Array.Empty<object>();
        }
    }

    // ── Top Processes ────────────────────────────────────
    private object[] GetTopProcesses()
    {
        try
        {
            var psi = new ProcessStartInfo("ps", "aux --sort=-%mem")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            var output = proc?.StandardOutput.ReadToEnd() ?? "";
            proc?.WaitForExit();

            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            // Skip header, take top 15
            return lines.Skip(1).Take(15).Select(line =>
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 11) return null;
                return (object)new
                {
                    user = parts[0],
                    pid = parts[1],
                    cpu = parts[2],
                    mem = parts[3],
                    command = string.Join(' ', parts.Skip(10))
                };
            }).Where(x => x != null).ToArray()!;
        }
        catch
        {
            return Array.Empty<object>();
        }
    }
}
