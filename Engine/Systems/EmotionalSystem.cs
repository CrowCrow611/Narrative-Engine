using Engine.Components;
using Engine.Events;

namespace Engine.Systems;

public record EmotionTCE(
    string EntityI,
    string EmotionName,
    float Intensity,
    float Threshold,
    List<string> Effects
);

public class EmotionalSystem
{
    private readonly EventBus? _bus;
    private readonly EffectDispatchSystem? _effects;

    public EmotionalSystem(EventBus? bus = null,
        EffectDispatchSystem? effects = null)
    {
        _bus = bus;
        _effects = effects;
    }

    public void Step(string entityId, EmotionalState state, float deltaTime)
    {
        foreach (var emotion in state.Emotions.Values)
        {
            var prev = emotion.Intensity;

            if (emotion.DecayRate > 0f)
                emotion.Intensity = Math.Clamp(
                    emotion.Intensity - emotion.DecayRate * deltaTime,
                    emotion.Min, emotion.Max);

            foreach (var threshold in emotion.Thresholds.ToList())
            {
                bool crossedDown = prev >= threshold.Intensity && emotion.Intensity <= threshold.Intensity;
                bool crossedUp = prev < threshold.Intensity && emotion.Intensity >= threshold.Intensity;

                if (crossedDown || crossedUp)
                {
                    _bus ?.Publish(new EmotionTCE(
                        entityId, emotion.Name,
                        emotion.Intensity, threshold.Intensity,
                        threshold.Effects));

                    if (_effects is not null)
                        foreach (var effect in threshold.Effects)
                            _effects.Apply(effect);
                }
            }    
        }
    }

    public void ApplyContagion(EmotionalState source, EmotionalState target,
        string emotionName, float strength)
    {
        if (!source.Has(emotionName)) return;
        if (!target.Has(emotionName)) return;

        var sourceIntensity = source.GetIntensity(emotionName);
        target.ModifyIntensity(emotionName, sourceIntensity * strength);
    }

    public void ApplyEffect(EmotionalState state, string effect)
    {
        var parts = effect.Trim().Split(' ');
        if (parts.Length != 3) throw new InvalidOperationException(
            $"Invalid emotion effect: '{effect}'");

        var path = parts[0].Split('.');
        if (path.Length != 3 || path[0] != "emotion")
            throw new InvalidOperationException(
                $"Invalid emotion effect path: '{parts[0]}'");

        var name = path[1];
        var property = path[2];
        var op = parts[1];
        var value = float.Parse(parts[2]);

        switch (property)
        {
            case "intensity":
                if (op == "=") state.SetIntensity(name, value);
                if (op == "+=") state.ModifyIntensity(name, value);
                if (op == "-=") state.ModifyIntensity(name, -value);
                break;

            case "decay":
                state.SetDecay(name, value);
                break;
            default:
                throw new InvalidOperationException(
                        $"Unknown emotion Property: '{property}'");
        }
    }
}