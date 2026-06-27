namespace Engine.Components;

public class WorldState {
    public Dictionary<string, bool> Flags { get; } = new();
    public Dictionary<string, int> Counters { get; } = new();
    public Dictionary<string, float> Timers { get; } = new();

    public void SetFlag(string key, bool value) => Flags[key] = value;
    public void SetCounter(string key, int value) => Counters[key] = value;
    public void SetTimer(string key, float value) => Timers[key] = value;

    public bool GetFlag(string key) => Flags.TryGetValue(key, out var v) && v;
    public int GetCounter(string key) => Counters.TryGetValue(key, out var v) ? v : 0;
    public float GetTimer(string key) => Timers.TryGetValue(key, out var v) ? v : 0f;
}