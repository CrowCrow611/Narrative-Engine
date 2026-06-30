namespace Engine.Parser;

public class ParseException : Exception
{
    public int Line { get; }
    public int Column { get; }

    public ParseException(string message, int line, int column)
        : base($"Error at line {line}, column {column}: {message}")
    {
        Line = line;
        Column = column;
    }
}