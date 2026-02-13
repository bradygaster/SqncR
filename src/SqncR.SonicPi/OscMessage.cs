using System.Text;

namespace SqncR.SonicPi;

/// <summary>
/// Minimal OSC message encoder for Sonic Pi communication.
/// Implements just enough of the OSC 1.0 spec to send string-argument messages.
/// </summary>
internal static class OscMessage
{
    /// <summary>
    /// Builds an OSC message with the given address and a single string argument.
    /// </summary>
    public static byte[] Create(string address, string argument)
    {
        using var ms = new MemoryStream();
        WriteOscString(ms, address);
        WriteOscString(ms, ",s"); // type tag: one string argument
        WriteOscString(ms, argument);
        return ms.ToArray();
    }

    /// <summary>
    /// Builds an OSC message with the given address, a GUID tag, and a string argument.
    /// Sonic Pi's /run-code expects (guid, code) arguments.
    /// </summary>
    public static byte[] CreateRunCode(string code)
    {
        using var ms = new MemoryStream();
        WriteOscString(ms, "/run-code");
        WriteOscString(ms, ",ss"); // type tag: two string arguments
        WriteOscString(ms, Guid.NewGuid().ToString());
        WriteOscString(ms, code);
        return ms.ToArray();
    }

    /// <summary>
    /// Builds an OSC message with no arguments (e.g., /stop-all-jobs).
    /// </summary>
    public static byte[] CreateNoArgs(string address)
    {
        using var ms = new MemoryStream();
        WriteOscString(ms, address);
        WriteOscString(ms, ","); // type tag: no arguments
        return ms.ToArray();
    }

    /// <summary>
    /// Writes a null-terminated, 4-byte-aligned OSC string to the stream.
    /// </summary>
    internal static void WriteOscString(Stream stream, string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
        // OSC strings are null-terminated and padded to 4-byte boundary
        int paddedLength = (bytes.Length + 4) & ~3; // round up to next multiple of 4
        int padding = paddedLength - bytes.Length;
        for (int i = 0; i < padding; i++)
            stream.WriteByte(0);
    }
}
