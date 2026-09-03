using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Channels;

namespace VoiceTrigger.Logging;

public sealed class LoggerService : IAsyncService
{
    public static readonly LoggerService Instance = new();

    static readonly string LogDirectory = Path.Combine(RoamingDirectory, "Logs");
    static readonly string LogFilePath = Path.Combine(LogDirectory, "now.log");
    static readonly string LogFileBackupFormat = Path.Combine(LogDirectory, "{0}.log");

    [MemberNotNullWhen(true, nameof(Identity))]
    [MemberNotNullWhen(true, nameof(InputQueue))]
    [MemberNotNullWhen(true, nameof(WorkerTask))]
    bool IsInitialized { get; set; }
    readonly Lock Lock = new();
    CancellationTokenSource? Identity;
    ChannelWriter<string>? InputQueue;
    Task? WorkerTask;

    public Task Initialize()
    {
        lock (Lock) InitializeCore();
        return Task.CompletedTask;
    }
    void InitializeCore()
    {
        if (!IsInitialized)
        {
            $"{this} Initializing...".Out();
            try
            {
                Directory.CreateDirectory(LogDirectory);
                var stream = new StreamWriter(new FileStream(LogFilePath, FileMode.Create, FileAccess.Write, FileShare.Read)) { AutoFlush = true };
                var channel = Channel.CreateUnbounded<string>();
                InputQueue = channel.Writer;
                Identity = new CancellationTokenSource();
                WorkerTask = LogWorker(Identity, stream, channel);
                IsInitialized = true;
                $"{this} Service initialized.".Out();
            }
            catch (Exception ex)
            {
                ex.Out($"{this} Failed to initialize! File logging will remain inactive.\n");
                IsInitialized = false;
                InputQueue = null;
                Identity?.Cancel();
                Identity = null;
                WorkerTask = null;
            }
        }
    }

    async Task LogWorker(CancellationTokenSource identity, StreamWriter? stream, Channel<string> channel)
    {
        await Task.Yield(); // Makes sure the service is fully initialized before the worker starts.
        ChannelWriter<string>? writer = null;
        try
        {
            ArgumentNullException.ThrowIfNull(stream);
            writer = channel.Writer;
            ChannelReader<string> reader = channel.Reader;

            const int MaxRestartAttempts = 10;
            int restarts = 0;
            CancellationToken token = identity.Token;
#if CONSOLE
            using var _ = token.Register(() => $"{this} [Worker] Active token cancelled.".Out());
#endif
            while (!token.IsCancellationRequested)
            {
                try
                {
                    $"{this} [Worker] Started.".Out(ConsoleColor.Gray);
                    //while (await reader.WaitToReadAsync(token))
                    //{
                    //    while (reader.TryRead(out string? line))
                    //    {
                    //        token.ThrowIfCancellationRequested();
                    //        await stream.WriteLineAsync(line.AsMemory(), token);
                    //    }
                    //}
                    // This implementation stalls the thread indefinitely, even if token is cancelled.
                    // This might be a bug or a quirk in API (likely the latter).
                    // Finding any solution that will not allow infinite blocking is proving to be hard as well.
                    await foreach (string line in reader.ReadAllAsync(token))
                    {
                        // No token - we allow it to write all the logs before quitting.
                        await stream.WriteLineAsync(line.AsMemory());
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex) { ex.Out($"{this} [Worker]"); }
                if (restarts > MaxRestartAttempts)
                {
                    $"{this} [Worker] Reached maximum amount of restart attempts ({MaxRestartAttempts})! Service will be terminated for this session.".Out(ConsoleColor.Yellow);
                    lock (Lock)
                    {
                        // Terminates the service if service is currently managing this specific worker.
                        if (IsInitialized && Identity == identity)
                            TerminateCore();
                    }
                    break;
                }

                restarts++;
                $"{this} [Worker] Restarting in {10}ms".Out(ConsoleColor.DarkYellow);
                try
                {
                    await Task.Delay(10, token);
                }
                catch (OperationCanceledException) { break; }
                $"{this} [Worker] Restarting...".Out(ConsoleColor.Yellow);
            }

            $"{this} [Worker] Stopping...".Out(ConsoleColor.Gray);


            stream.Close();
            stream = null;
            writer.TryComplete();
            writer = null;

            const int MaxRenamingAttempts = 1024;
            int attempt = 0;
            for (; attempt < MaxRenamingAttempts; attempt++)
            {
                string suffix = attempt == 0 ? "" : $" ({attempt})";
                string file = LogFileBackupFormat.Replace("{0}", $"{DateTime.Now:yyyy-MM-dd HH-mm-ss-fff}{suffix}");
                if (File.Exists(file)) continue;
                try
                {
                    File.Move(LogFilePath, file);
                }
                catch (Exception ex) { ex.Out($"{this} Failed to move current log file!\n"); }
                break;
            }

            if (attempt >= MaxRenamingAttempts)
            {
                $"{this} [Worker] Reached max log file renaming attempts. Log file will remain with its default name and so will be overwritten on the next session.".Out(ConsoleColor.Yellow);
            }

            $"{this} [Worker] Stopped.".Out(ConsoleColor.Gray);
        }
        catch (Exception ex) { ex.Out($"{this} [Worker]"); }
        finally { writer?.TryComplete(); stream?.Dispose(); }
    }

    public async Task Terminate()
    {
        Task? worker = null;
        lock (Lock)
        {
            if (IsInitialized)
            {
                worker = WorkerTask;
                TerminateCore();
            }
        }
        if (worker is not null)
        {
            $"{this} Forcing worker to syncronize with the app...".Out();
            $"{this} (App stalling infinitely here is a critical bug)".Out(ConsoleColor.Yellow);
            // Should only ever happen after the identiy cancelling, and outside of a lock, as it will otherwise lead to an infinite stall.
            try
            {
                await worker;
            }
            catch (Exception ex) { ex.Out($"{this} Exception while syncronizing a worker thread with UI thread!"); }
        }
    }
    void TerminateCore()
    {
        if (IsInitialized)
        {
            $"{this} Terminating...".Out();
            IsInitialized = false;
            InputQueue?.TryComplete();
            InputQueue = null;
            Identity?.Cancel();
            Identity = null;
            WorkerTask = null;
            $"{this} Service terminated.".Out();
        }
    }

    public void Log(string? content)
    {
        ChannelWriter<string>? writer;
        lock (Lock)
        {
            if (!IsInitialized)
                return;
            writer = InputQueue;
        }

        try
        {
            string timestamp = $"[{DateTime.Now:HH:mm:ss.fff}] ";
            if (string.IsNullOrEmpty(content))
            {
                writer.TryWrite(timestamp);
                return;
            }

            var span = content.AsSpan();
            int head = 0;
            int length = span.Length;
            bool lastWasCarret = false;
            for (int i = 0; i < length; i++)
            {
                switch (span[i])
                {
                    case '\n':
                        if (lastWasCarret)
                        {
                            lastWasCarret = false;
                            continue;
                        }
                        break;

                    case '\r':
                        lastWasCarret = true;
                        break;

                    default: continue;
                }

                int total = i - head;
                writer.TryWrite(string.Concat(timestamp, span.Slice(head, total)));
                head = i + 1;
            }

            int remaining = length - head;
            writer.TryWrite(string.Concat(timestamp, span.Slice(head, remaining)));
        }
        catch (Exception ex) { Console.WriteLine(ToString() + " " + ex.ToDisplay()); }
    }

    public override string ToString() => $"[{nameof(LoggerService)}]";
}
