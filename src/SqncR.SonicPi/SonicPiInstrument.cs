namespace SqncR.SonicPi;

/// <summary>
/// Represents a Sonic Pi instrument — a software synth voice controlled via OSC.
/// Maps to the unified Instrument abstraction from the architecture.
/// </summary>
public class SonicPiInstrument
{
    public SonicPiInstrument(string name, string synthName, int channel = 1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(synthName);

        Name = name;
        SynthName = synthName;
        Channel = channel;
    }

    public string Name { get; }
    public string SynthName { get; }
    public int Channel { get; }
    public Dictionary<string, double> FxChain { get; set; } = new();
    public bool IsActive { get; private set; }

    /// <summary>
    /// Sends the synth setup code to Sonic Pi, activating this instrument.
    /// </summary>
    public void Activate(OscClient client)
    {
        ArgumentNullException.ThrowIfNull(client);

        var code = RubyCodeGenerator.GenerateSynthSetup(SynthName);
        if (FxChain.Count > 0)
        {
            code += "\n" + RubyCodeGenerator.GenerateFxChain(FxChain);
        }

        client.SendCode(code);
        IsActive = true;
    }

    /// <summary>
    /// Sends a stop command to Sonic Pi, deactivating this instrument.
    /// </summary>
    public void Deactivate(OscClient client)
    {
        ArgumentNullException.ThrowIfNull(client);

        client.StopAll();
        IsActive = false;
    }
}
