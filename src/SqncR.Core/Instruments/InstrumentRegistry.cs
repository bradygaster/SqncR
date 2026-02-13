using System.Collections.Concurrent;

namespace SqncR.Core.Instruments;

/// <summary>
/// Thread-safe registry of active instruments. Manages add/remove/query operations.
/// </summary>
public sealed class InstrumentRegistry
{
    private readonly ConcurrentDictionary<string, Instrument> _instruments = new();

    /// <summary>Register an instrument. Throws if an instrument with the same id already exists.</summary>
    public void Add(Instrument instrument)
    {
        ArgumentNullException.ThrowIfNull(instrument);

        if (!_instruments.TryAdd(instrument.Id, instrument))
            throw new ArgumentException($"An instrument with id '{instrument.Id}' is already registered.");
    }

    /// <summary>Unregister an instrument by id. Returns true if found and removed.</summary>
    public bool Remove(string id) => _instruments.TryRemove(id, out _);

    /// <summary>Get an instrument by id, or null if not found.</summary>
    public Instrument? Get(string id) => _instruments.TryGetValue(id, out var inst) ? inst : null;

    /// <summary>Get all instruments matching a given role.</summary>
    public IReadOnlyList<Instrument> GetByRole(InstrumentRole role) =>
        _instruments.Values.Where(i => i.Role == role).ToList().AsReadOnly();

    /// <summary>Get all registered instruments.</summary>
    public IReadOnlyList<Instrument> GetAll() =>
        _instruments.Values.ToList().AsReadOnly();
}
