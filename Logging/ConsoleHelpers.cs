using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;

namespace VoiceTrigger.Logging;

public static class ConsoleHelpers
{
    static readonly ConcurrentStack<StringBuilder> Builders = [];
    public static string ToDisplay(this Exception ex) => $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Out(this Exception ex, ConsoleColor color = ConsoleColor.Red) => ex.Out((object?)null, color);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Out(this Exception ex, string? prefix, ConsoleColor color = ConsoleColor.Red) => ex.Out((object?)prefix, color);
    public static void Out(this Exception ex, object? prefix, ConsoleColor color = ConsoleColor.Red)
    {
        StringBuilder builder = AquireBuilder();
        try
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
                    if (!str.EndsWith(' ') && !str.EndsWith('\t'))
                        builder.Append(' ');
                }
            }
            builder.Append(ex.ToDisplay());
            string final = builder.ToString();
            builder.Clear();

            Console.WriteLine(final);
            Console.ForegroundColor = last;
            LoggerService.Instance.Log(final);
        }
        catch (Exception ex2) { Console.WriteLine(ex2.ToDisplay()); }
        finally { ReturnBuilder(builder); }
    }

    public static void Out(this object? obj, ConsoleColor color = ConsoleColor.White)
    {
        StringBuilder builder = AquireBuilder();
        try
        {
            ConsoleColor last = Console.ForegroundColor;
            Console.ForegroundColor = color;

            builder.Clear();
            builder.Append((obj ?? "null").ToString());
            string final = builder.ToString();
            builder.Clear();

            Console.WriteLine(final);
            Console.ForegroundColor = last;
            LoggerService.Instance.Log(final);
        }
        catch (Exception ex) { Console.WriteLine(ex.ToDisplay()); }
        finally { ReturnBuilder(builder); }
    }

    public static void Out(this object? obj, string? prefix, ConsoleColor color = ConsoleColor.White)
    {
        StringBuilder builder = AquireBuilder();
        try
        {
            ConsoleColor last = Console.ForegroundColor;
            Console.ForegroundColor = color;

            builder.Clear();
            if (prefix is not null)
            {
                builder.Append(prefix);
                if (!prefix.EndsWith(' ') && !prefix.EndsWith('\t'))
                    builder.Append(' ');
            }
            builder.Append((obj ?? "null").ToString());
            string final = builder.ToString();
            builder.Clear();

            Console.WriteLine(final);
            Console.ForegroundColor = last;
            LoggerService.Instance.Log(final);
        }
        catch (Exception ex) { Console.WriteLine(ex.ToDisplay()); }
        finally { ReturnBuilder(builder); }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static StringBuilder AquireBuilder() => Builders.TryPop(out var builder) ? builder : new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void ReturnBuilder(StringBuilder builder) => Builders.Push(builder);
    //static StringBuilder AquireBuilder() => Builders.GetOrAdd(Environment.CurrentManagedThreadId, static _ => new());
}
