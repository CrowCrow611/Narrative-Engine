using Engine.AST;

namespace Engine.Rendering;

public interface IRenderer {
    void RenderNode(StoryNode node);
    void RenderError(string message);
}