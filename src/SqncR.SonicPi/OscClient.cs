using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace SqncR.SonicPi;

/// <summary>
/// Lightweight OSC client for communicating with Sonic Pi.
/// Sends UDP messages to localhost on the configured port (default 4560).
/// </summary>
public class OscClient : IDisposable
{
    internal static readonly ActivitySource ActivitySource = new("SqncR.SonicPi");

    private readonly UdpClient _udpClient;
    private readonly IPEndPoint _endpoint;
    private bool _disposed;

    public OscClient(int port = 4560, string host = "127.0.0.1")
    {
        Port = port;
        Host = host;
        _udpClient = new UdpClient();
        _endpoint = new IPEndPoint(IPAddress.Parse(host), port);
    }

    public int Port { get; }
    public string Host { get; }

    /// <summary>
    /// Sends Ruby code to Sonic Pi via the /run-code OSC endpoint.
    /// </summary>
    public void SendCode(string rubyCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rubyCode);

        using var activity = ActivitySource.StartActivity("sonicpi.send_code");
        activity?.SetTag("osc.port", Port);
        activity?.SetTag("osc.endpoint", $"{Host}:{Port}");
        activity?.SetTag("sonicpi.code_length", rubyCode.Length);

        var message = OscMessage.CreateRunCode(rubyCode);
        activity?.SetTag("osc.message_size_bytes", message.Length);
        Send(message);
    }

    /// <summary>
    /// Sends /stop-all-jobs to silence all running Sonic Pi code.
    /// </summary>
    public void StopAll()
    {
        using var activity = ActivitySource.StartActivity("sonicpi.stop_all");
        activity?.SetTag("osc.port", Port);
        activity?.SetTag("osc.endpoint", $"{Host}:{Port}");

        var message = OscMessage.CreateNoArgs("/stop-all-jobs");
        activity?.SetTag("osc.message_size_bytes", message.Length);
        Send(message);
    }

    /// <summary>
    /// Best-effort check if Sonic Pi is listening.
    /// UDP is connectionless, so this only verifies the port is reachable.
    /// </summary>
    public bool IsAvailable()
    {
        try
        {
            using var probe = new UdpClient();
            // Send a no-op ping; Sonic Pi will ignore unknown addresses
            var message = OscMessage.CreateNoArgs("/ping");
            probe.Send(message, message.Length, _endpoint);
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    private void Send(byte[] message)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        _udpClient.Send(message, message.Length, _endpoint);
        sw.Stop();

        SonicPiMetrics.OscMessagesSent.Add(1);
        SonicPiMetrics.OscLatency.Record(sw.Elapsed.TotalMicroseconds);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _udpClient.Dispose();
        }
    }
}
