namespace SqncR.Core.Generation;

/// <summary>
/// A single planned note to be emitted by the engine on a specific MIDI channel.
/// </summary>
public record PlannedNote(int Channel, int Note, int Velocity, int DurationTicks, string? InstrumentId = null);

/// <summary>
/// Per-tick generation plan: the set of notes to emit across all channels.
/// </summary>
public record ChannelPlan(IReadOnlyList<PlannedNote> Notes);
