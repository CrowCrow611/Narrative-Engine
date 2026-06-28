using Engine.AST;

namespace Engine.Rendering;

public class TerminalRenderer : IRenderer
{
    private readonly bool _debugMode;

    public TerminalRenderer(bool debugMode = false)
    {
        _debugMode = debugMode;
    }

public void RenderNode(StoryNode node)
    {
        Console.WriteLine();

        if (_debugMode)
            WriteColored($"[{node.Kind}: {node.Id}]", ConsoleColor.DarkGray);

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
                WriteColored($"[{node.Kind}: {node.Id}]", ConsoleColor.DarkGray);
                break;
        }
    }

    private void RenderBeat(StoryNode node)
    {
        var speaker = node.Tags.FirstOrDefault(t => t.StartsWith("speaker:"));
        if (speaker is not null)
        {
            var name = speaker.Substring(8).Trim();
            WriteColored($" {name}", ConsoleColor.Yellow);
            WriteColored(" " + new string('-', name.Length), ConsoleColor.DarkYellow);
        }

        var emotion = node.Tags.FirstOrDefault(t => t.StartsWith("emotion:"));
        if (emotion is not null)
            WriteColored($" [{emotion.Substring(8).Trim()}]", ConsoleColor.DarkCyan);

        var text = node.Text ?? "";
        var width = Math.Min(Console.WindowWidth - 4, 80);
        var wrapped = WrapText(text, width - 4);

        WriteColored("  ┌" + new string('─', width - 2) + "┐", ConsoleColor.DarkGray);
        foreach (var line in wrapped)
            WriteColored($"  │ {line.PadRight(width - 4)} │", ConsoleColor.DarkGray, line, ConsoleColor.White);
        WriteColored("  └" + new string('─', width - 2) + "┘", ConsoleColor.DarkGray);
    }

    private void RenderChoice(StoryNode node)
    {
        if (node.Text is not null)
            RenderBeat(node with { Kind = NodeKind.Beat});

        Console.WriteLine();
        WriteColored("  ╔══ Choose ══╗", ConsoleColor.Cyan);
        for (int i = 0; i < node.Branches.Count; i++)
        {
            var branch = node.Branches[i];
            var locked = branch.Condition is not null;
            var color = locked ? ConsoleColor.DarkGray : ConsoleColor.White;
            var prefix = locked ? " || 🔓" : $" || [{i + 1}]";
            WriteColored($"{prefix} {branch.Text}", color);
        }

        WriteColored("  ╚════════════╝", ConsoleColor.Cyan);
        Console.Write("\n > ");
    }

    private void RenderSceneHeader(StoryNode node)
    {
        var title = node.Text ?? node.Id;
        var line = new string('=', title.Length + 4);
        WriteColored($"  ╔{line}╗", ConsoleColor.Magenta);
        WriteColored($"  ║  {title}  ║", ConsoleColor.Magenta);
        WriteColored($"  ╚{line}╝", ConsoleColor.Magenta);
    }

    public void RenderError(string message)
    {
        WriteColored($"  x {message}", ConsoleColor.Red);
    }

    private static void WriteColored(string line, ConsoleColor color, 
        string? highlight = null, ConsoleColor hihglightColor = ConsoleColor.White)
    {
        if (highlight is null)
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(line);
            Console.ForegroundColor = prev;
            return;
        }

        var idx = line.IndexOf(highlight);
        if (idx < 0) { WriteColored(line, color); return; }

        var prev2 = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(line[..idx]);
        Console.ForegroundColor = hihglightColor;
        Console.Write(highlight);
        Console.ForegroundColor = color;
        Console.WriteLine(line[(idx + highlight.Length)..]);
        Console.ForegroundColor = prev2;
    }

    private static List<string> WrapText(string text, int width)
    {
        var words = text.Split(' ');
        var lines = new List<string>();
        var current = new System.Text.StringBuilder();

        foreach (var word in words)
        {
            if (current.Length + word.Length + 1 > width)
            {
                lines.Add(current.ToString().TrimEnd());
                current.Clear();
            }
            current.Append(word + " ");
        }

        if (current.Length > 0)
            lines.Add(current.ToString().TrimEnd());

        return lines;
    }
}