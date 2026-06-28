using Engine.AST;
using System.Text;

namespace Engine.Rendering;

public class BrowserRenderer : IRenderer
{
    private readonly StringBuilder _buffer = new();
    private readonly bool _debugMode;

    public BrowserRenderer(bool debugMode = false)
    { 
        _debugMode = debugMode;
    }
    public string FlushHtml()
    {
        var html = _buffer.ToString();
        _buffer.Clear();
        return html;
    }

    public void RenderNode(StoryNode node)
    {
        if (_debugMode)
            _buffer.AppendLine(
                 $"<div class=\"debug-info\" data-kind=\"{node.Kind}\" data-node=\"{node.Id}\">" +
                $"{node.Kind}: {node.Id}</div>");

        switch (node.Kind)
        {
            case NodeKind.Beat:
                RenderBeat(node);
                break;

            case NodeKind.Choice:
                RenderChoice(node);
                break;

            case NodeKind.Scene:
            case NodeKind.Chapter:
                RenderSceneHeader(node);
                break;

            default:
                _buffer.AppendLine(
                    $"<div class=\"node\" data-kind=\"{node.Kind}\" data-node=\"{node.Id}\"></div>");
                break;
          }  
        
        }
        private void RenderBeat(StoryNode node)
    {
         var speaker = node.Tags.FirstOrDefault(t => t.StartsWith("speaker:"));
         var emotion = node.Tags.FirstOrDefault(t => t.StartsWith("emotion:"));

         _buffer.AppendLine($"<div class=\"beat\" data-node=\"{node.Id}\">");

         if (speaker is not null)
        {
            var name = Escape(speaker.Substring(8).Trim());
            _buffer.AppendLine($"<span class=\"speaker\" data-speaker=\"{name}\">{name}</span>");
        }

        if (emotion is not null)
        {
            var emo = Escape(emotion.Substring(8).Trim());
            _buffer.AppendLine($"<span class=\"emotion\" data-emotion=\"{emo}\">{emo}</span>");
        }
         _buffer.AppendLine($"  <p class=\"beat-text\">{Escape(node.Text ?? "")}</p>");
         _buffer.AppendLine("</div>");
    }

    private void RenderChoice(StoryNode node)
    {
        if (node.Text is not null) 
            RenderBeat(node with { Kind = NodeKind.Beat });

        _buffer.AppendLine($"<div class=\"choice-block\" data-node=\"{node.Id}\">");
        _buffer.AppendLine("<ol class=\"choices\">");

        for (int i = 0; i < node.Branches.Count; i++)
        {
            var branch = node.Branches[i];
            var locked = branch.Condition is not null;
            var cls = locked ? "choice locked" : "choice";
            var cond = branch.Condition is not null 
                ? $" data-condition=\"{Escape(branch.Condition)}\"" : "";
            _buffer.AppendLine(
                $"<li class=\"{cls}\" data-index=\"{i}\"{cond}>" +
                $"{Escape(branch.Text)}</li>");
        }

        _buffer.AppendLine("<ol>");
        _buffer.AppendLine("</div>");
    }

    private void RenderSceneHeader(StoryNode node)
    {
        var title = Escape(node.Text ?? node.Id);
        _buffer.AppendLine(
            $"<header class=\"scene-header\" data-node=\"{node.Id}\">" +
            $"<h2>{title}</h2></header>");
    }

    public void RenderError(string message)
    {
        _buffer.AppendLine(
            $"<p class=\"error\" role=\"alert\">{Escape(message)}</p>");
    }

    private static string Escape(string text) => 
        text.Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
}
