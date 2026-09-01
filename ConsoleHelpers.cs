using System.Runtime.CompilerServices;
using System.Text;
using VoiceTrigger.Services;

namespace VoiceTrigger;

public static class ConsoleHelpers
{
    static readonly StringBuilder builder = new();
    public static string ToDisplay(this Exception ex) => $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Out(this Exception ex, string? prefix = null, ConsoleColor color = ConsoleColor.Red)
    {
        ex.Out((object?)prefix, color);
    }
    public static void Out(this Exception ex, object? prefix = null, ConsoleColor color = ConsoleColor.Red)
    {
        lock (builder)
        {
            ConsoleColor last = Console.ForegroundColor;
            Console.ForegroundColor = color;

            builder.Clear();
            if (prefix is not null)
            {
                string? str = prefix.ToString();
                if (str is not null)
                {
                    builder.Append(str);
                    if (!str.EndsWith(' '))
                        builder.Append(' ');
                }
            }
            builder.Append(ex.ToDisplay());
            string final = builder.ToString();
            builder.Clear();

            Console.WriteLine(final);
            Console.ForegroundColor = last;
            LogService.Log(final);
        }
    }

    public static void Out(this object? obj, ConsoleColor color = ConsoleColor.White)
    {
        lock (builder)
        {
            ConsoleColor last = Console.ForegroundColor;
            Console.ForegroundColor = color;

            builder.Clear();
            builder.Append((obj ?? "null").ToString());
            string final = builder.ToString();
            builder.Clear();

            Console.WriteLine(final);
            Console.ForegroundColor = last;
            LogService.Log(final);
        }
    }

    public static void Out(this object? obj, string? prefix, ConsoleColor color = ConsoleColor.White)
    {
        lock (builder)
        {
            ConsoleColor last = Console.ForegroundColor;
            Console.ForegroundColor = color;

            builder.Clear();
            if (prefix is not null)
            {
                builder.Append(prefix);
                if (!prefix.EndsWith(' '))
                    builder.Append(' ');
            }
            builder.Append((obj ?? "null").ToString());
            string final = builder.ToString();
            builder.Clear();

            Console.WriteLine(final);
            Console.ForegroundColor = last;
            LogService.Log(final);
        }
    }
}
