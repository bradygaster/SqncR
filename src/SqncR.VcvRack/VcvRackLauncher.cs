using System.Diagnostics;

namespace SqncR.VcvRack;

/// <summary>
/// Manages launching and stopping VCV Rack 2 with a patch file.
/// Detects common install locations on Windows.
/// </summary>
public class VcvRackLauncher : IDisposable
{
    internal static readonly ActivitySource ActivitySource = new("SqncR.VcvRack");

    private Process? _process;
    private bool _disposed;

    /// <summary>
    /// Default virtual MIDI port name used by loopMIDI on Windows.
    /// </summary>
    public string MidiPortName { get; set; } = "loopMIDI Port";

    /// <summary>
    /// Whether VCV Rack is currently running.
    /// </summary>
    public bool IsRunning => _process is { HasExited: false };

    /// <summary>
    /// Launches VCV Rack with the specified patch file.
    /// </summary>
    public async Task LaunchAsync(string patchPath, bool headless = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(patchPath);

        if (IsRunning)
            throw new InvalidOperationException("VCV Rack is already running. Call StopAsync() first.");

        using var activity = ActivitySource.StartActivity("vcvrack.launch");
        activity?.SetTag("vcvrack.patch", patchPath);
        activity?.SetTag("vcvrack.headless", headless);
        activity?.SetTag("vcvrack.midi_port", MidiPortName);

        var rackPath = FindRackExecutable();
        if (rackPath is null)
            throw new FileNotFoundException(
                "VCV Rack 2 executable not found. Install VCV Rack 2 or set the path manually.");

        var args = headless ? $"-h \"{patchPath}\"" : $"\"{patchPath}\"";

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = rackPath,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = headless
            }
        };

        _process.Start();

        // Brief delay to let VCV Rack initialize
        await Task.Delay(500);

        activity?.SetTag("vcvrack.pid", _process.Id);
    }

    /// <summary>
    /// Stops the running VCV Rack process.
    /// </summary>
    public async Task StopAsync()
    {
        using var activity = ActivitySource.StartActivity("vcvrack.stop");

        if (_process is null || _process.HasExited)
            return;

        try
        {
            _process.Kill(entireProcessTree: true);
            await _process.WaitForExitAsync();
        }
        catch (InvalidOperationException)
        {
            // Process already exited
        }
        finally
        {
            _process.Dispose();
            _process = null;
        }
    }

    /// <summary>
    /// Searches common install locations for the VCV Rack executable.
    /// </summary>
    internal static string? FindRackExecutable()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "VCV", "Rack2Free", "Rack.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "VCV", "Rack2", "Rack.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Rack2", "Rack.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents", "Rack2", "Rack.exe"),
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            if (_process is { HasExited: false })
            {
                try { _process.Kill(entireProcessTree: true); } catch { /* best effort */ }
            }
            _process?.Dispose();
        }
    }
}
