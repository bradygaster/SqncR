using System.Text;

namespace SqncR.SonicPi.Tests;

public class OscClientTests
{
    [Fact]
    public void Constructor_DefaultPort_Is4560()
    {
        using var client = new OscClient();

        Assert.Equal(4560, client.Port);
        Assert.Equal("127.0.0.1", client.Host);
    }

    [Fact]
    public void Constructor_CustomPort_IsStored()
    {
        using var client = new OscClient(port: 5000);

        Assert.Equal(5000, client.Port);
    }

    [Fact]
    public void SendCode_NullOrEmpty_ThrowsArgumentException()
    {
        using var client = new OscClient();

        Assert.Throws<ArgumentException>(() => client.SendCode(""));
        Assert.Throws<ArgumentException>(() => client.SendCode("   "));
        Assert.Throws<ArgumentNullException>(() => client.SendCode(null!));
    }

    [Fact]
    public void Dispose_MultipleCalls_DoesNotThrow()
    {
        var client = new OscClient();
        client.Dispose();
        client.Dispose(); // second dispose should not throw
    }

    [Fact]
    public void OscMessage_CreateRunCode_HasCorrectStructure()
    {
        var message = OscMessage.CreateRunCode("play 60");

        // The message should start with the /run-code address
        var addressEnd = Array.IndexOf(message, (byte)0);
        var address = Encoding.ASCII.GetString(message, 0, addressEnd);
        Assert.Equal("/run-code", address);

        // Should contain type tag ",ss"
        var messageStr = Encoding.ASCII.GetString(message);
        Assert.Contains(",ss", messageStr);

        // Should contain the code
        Assert.Contains("play 60", messageStr);
    }

    [Fact]
    public void OscMessage_CreateNoArgs_HasCorrectFormat()
    {
        var message = OscMessage.CreateNoArgs("/stop-all-jobs");

        var addressEnd = Array.IndexOf(message, (byte)0);
        var address = Encoding.ASCII.GetString(message, 0, addressEnd);
        Assert.Equal("/stop-all-jobs", address);
    }

    [Fact]
    public void OscMessage_StringPadding_IsAlignedTo4Bytes()
    {
        // "/run-code" is 9 chars + null = 10 bytes, padded to 12
        var message = OscMessage.CreateNoArgs("/run-code");
        // Total length should be a multiple of 4
        Assert.Equal(0, message.Length % 4);
    }
}
