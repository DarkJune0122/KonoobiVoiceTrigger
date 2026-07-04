namespace VoiceTrigger;

public static class ConsoleHelpers
{
    public static string ToDisplay(this Exception ex) => $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
    public static void Out(this Exception ex, string? prefix = null, ConsoleColor color = ConsoleColor.Red)
    {
        ConsoleColor last = Console.ForegroundColor;
        Console.ForegroundColor = color;
        if (prefix is not null)
        {
            Console.Write(prefix);
            if (!prefix.EndsWith(' '))
                Console.Write(' ');
        }
        Console.WriteLine(ex.ToDisplay());
        Console.ForegroundColor = last;
    }

    public static void Out(this object? obj, ConsoleColor color = ConsoleColor.White)
    {
        ConsoleColor last = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(obj);
        Console.ForegroundColor = last;
    }

    public static void Out(this object? obj, string? prefix, ConsoleColor color = ConsoleColor.White)
    {
        ConsoleColor last = Console.ForegroundColor;
        Console.ForegroundColor = color;
        if (prefix is not null)
        {
            Console.Write(prefix);
            if (!prefix.EndsWith(' '))
                Console.Write(' ');
        }
        Console.WriteLine(obj);
        Console.ForegroundColor = last;
    }
}
