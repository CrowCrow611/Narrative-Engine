namespace Engine.Components;

public enum RenderTarget { Browser, Terminal }
public enum RenderMode { Raw, Themed, Plain }

public class Renderable {
    public RenderTarget Target { get; set; } = RenderTarget.Terminal;
    public RenderMode Mode { get; set; } = RenderMode.Plain;
    public string Theme { get; set; } = "default";
}