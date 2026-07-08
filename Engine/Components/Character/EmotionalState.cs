namespace Engine.Components;

public record EmotionalThreshold(
    float Intensity,
    List<string> Effects,
    bool Triggered = false
);

public class EmotionDefinition
{
    public string Name { get; }
    public float Intensity { get; set; } = 0f;
    public float DecayRate { get; set; } = 0f;
    public float Min { get; set; } = 0f;
    public float Max { get; set; } = 0f;
    public List<EmotionalThreshold> Thresholds { get; } = new();

    public EmotionDefinition(string name) => Name = name;

}

public class EmotionalState
{
    private readonly Dictionary<string, EmotionDefinition> _emotions = new();
    public IReadOnlyDictionary<string, EmotionDefinition> Emotions => _emotions;

    public void Define(string name, float decayRate = 0f,
        float min = 0f, float max = 100f)
    {
        if (!_emotions.ContainsKey(name))
            _emotions[name] = new EmotionDefinition(name)
            {
                DecayRate = decayRate,
                Min = min,
                Max = max
            };
    }

    public void Remove(string name) => _emotions.Remove(name);
    public bool Has(string name) => _emotions.ContainsKey(name);

    public EmotionDefinition? Get(string name) => _emotions.TryGetValue(name, out var e) ? e : null;

    public void SetIntensity(string name, float value)
    {
        if (_emotions.TryGetValue(name, out var e))
            e.Intensity = Math.Clamp(value, e.Min, e.Max);
    }

    public void ModifyIntensity(string name, float delta)
    {
        if (_emotions.TryGetValue(name, out var e))
            e.Intensity = Math.Clamp(e.Intensity + delta, e.Min, e.Max);
    }

    public void SetDecay(string name, float rate)
    {
        if (_emotions.TryGetValue(name, out var e)) 
            e.DecayRate  = rate;
    }

    public void AddThreshold(string name, float intensity, List<string> effects)
    {
        if (_emotions.TryGetValue(name, out var e))
            e.Thresholds.Add(new EmotionalThreshold(intensity, effects));
    }

    public void RemoveThreshold(string name, float intensity)
    {
        if (_emotions.TryGetValue(name, out var e))
            e.Thresholds.RemoveAll(t => t.Intensity == intensity);
    }

    public float GetIntensity(string name) => 
        _emotions.TryGetValue(name, out var e) ? e.Intensity : 0f;
}