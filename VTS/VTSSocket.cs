using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS;

public sealed class VTSSocket : IDisposable
{
    const int InitialBufferSize = 1024 * 64;
    const int MaxBufferSize = 1024 * 1024 * 16;
    public delegate void StartHandler(VTSSocket client);
    public delegate void ReceiveHandler(VTSSocket client, ReadOnlySpan<char> message);
    public delegate void StopHandler(VTSSocket client);
    public readonly CancellationTokenSource Identity;
    public readonly CancellationToken Token;
    public readonly ClientWebSocket WebSocket;
    public readonly Encoding Encoding;

    byte[] Buffer = ArrayPool<byte>.Shared.Rent(InitialBufferSize);
    readonly SemaphoreSlim ReceiveSemaphore = new(1); // WARNING! Mutexes here break in Async code! Replace with SemaphoreSlim!
    readonly SemaphoreSlim SendSemaphore = new(1); // WARNING! Mutexes here break in Async code! Replace with SemaphoreSlim!
    readonly Lock Lock = new();
    volatile bool IsStarted;
    volatile bool Disposed;

    public VTSSocket(Encoding? encoding = null)
    {
        Encoding = encoding ?? Encoding.UTF8;
        Identity = new();
        Token = Identity.Token;
        WebSocket = new();
    }

    public Task ConnectAsync(Uri uri)
    {
        using (Lock.EnterScope())
        {
            Token.ThrowIfCancellationRequested();
            ObjectDisposedException.ThrowIf(Disposed, this);
            AlreadyStartedException.ThrowIf(IsStarted, this);
            IsStarted = true;
        }

        return WebSocket.ConnectAsync(uri, Token);
    }

    public ValueTask SendAsync(string json)
    {
        $"Sending:\n{json}".Out(ConsoleColor.Magenta);
        return SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true);
    }

    ValueTask SendAsync(ReadOnlySpan<byte> bytes, WebSocketMessageType type, bool endOfMessage)
    {
        using (Lock.EnterScope())
        {
            Token.ThrowIfCancellationRequested();
            ObjectDisposedException.ThrowIf(Disposed, this);
            NotStartedException.ThrowIf(!IsStarted, this);
        }

        byte[] buffer = ArrayPool<byte>.Shared.Rent(bytes.Length);
        bytes.CopyTo(buffer);
        return SendAsyncCore(buffer, bytes.Length, type, endOfMessage);
    }

    async ValueTask SendAsyncCore(byte[] bytes, int length, WebSocketMessageType type, bool endOfMessage)
    {
        await SendSemaphore.WaitAsync();
        try
        {
            await WebSocket.SendAsync(new Memory<byte>(bytes, 0, length), type, endOfMessage, Token);
        }
        finally
        {
            SendSemaphore.Release();
            ArrayPool<byte>.Shared.Return(bytes);
        }
    }

    public readonly struct ReceiveResult(bool success, char[]? buffer, int length) : IDisposable
    {
        public static readonly ReceiveResult Faulted = new(false, null, 0);
        public readonly bool Success = success;
        public Memory<char> Message => (buffer ?? throw new InvalidOperationException("Result doesn't contain a message.")).AsMemory(0, length);
        public void Dispose()
        {
            if (buffer is not null)
                ArrayPool<char>.Shared.Return(buffer);
        }
    }

    public async Task<T?> ReceiveAsync<T>()
    {
        ReceiveResult result = await ReceiveAsync();
        if (!result.Success)
        {
            $"Failed to receive ({typeof(T)}) from the server!".Out();
        }

        try
        {
            T? data = JsonSerializer.Deserialize<T>(result.Message.Span, VTSPackets.JsonOptions);
            $"Received:\n{data}".Out(ConsoleColor.Cyan);
            return data;
        }
        catch (JsonException)
        {
            $"Failed to deserialize {typeof(T)} from data:\n{new string(result.Message.Span)}".Out();
        }
        finally
        {
            result.Dispose();
        }

        return default;
    }

    public async Task<ReceiveResult> ReceiveAsync()
    {
        using (Lock.EnterScope())
        {
            Token.ThrowIfCancellationRequested();
            ObjectDisposedException.ThrowIf(Disposed, this);
            NotStartedException.ThrowIf(!IsStarted, this);
        }

        await ReceiveSemaphore.WaitAsync();
        char[]? chars = null;
        int head = 0;
        try
        {
            while (!Token.IsCancellationRequested)
            {
                var result = await WebSocket.ReceiveAsync(new(Buffer, head, Buffer.Length - head), Token);
                switch (result.MessageType)
                {
                    case WebSocketMessageType.Close:
                        await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", Token);
                        $"{this} VTube Studio gracefully closed connection.".Out(ConsoleColor.Yellow);
                        return ReceiveResult.Faulted;

                    case WebSocketMessageType.Binary: $"{this} Reading binary is not supported.".Out(ConsoleColor.Yellow); break;
                    case WebSocketMessageType.Text when !result.EndOfMessage:
                        head += result.Count;
                        if (head < Buffer.Length) break;

                        int newLength = checked(Buffer.Length * 2);
                        if (newLength > MaxBufferSize)
                        {
                            $"Message is too big! (over {head}/{MaxBufferSize} bytes)".Out();
                            await WebSocket.CloseAsync(WebSocketCloseStatus.MessageTooBig, "", Token);
                            return ReceiveResult.Faulted;
                        }

                        var temp = ArrayPool<byte>.Shared.Rent(newLength);
                        System.Buffer.BlockCopy(Buffer, 0, temp, 0, Buffer.Length);
                        ArrayPool<byte>.Shared.Return(Buffer);
                        Buffer = temp;
                        break;

                    case WebSocketMessageType.Text:
                        int length = checked(head + result.Count);
                        head = 0;

                        var bytes = Buffer.AsSpan(0, length);
                        chars = ArrayPool<char>.Shared.Rent(Encoding.GetCharCount(bytes));
                        int total = Encoding.GetChars(bytes, chars);
                        ReceiveResult ret = new(true, chars, total);
                        $"Received:\n{ret.Message}".Out(ConsoleColor.Cyan);
                        chars = null;
                        return ret;
                }
            }
        }
        finally
        {
            ReceiveSemaphore.Release();
            if (chars is not null)
                ArrayPool<char>.Shared.Return(chars);
        }

        return ReceiveResult.Faulted;
    }

    public void Dispose()
    {
        using (Lock.EnterScope())
        {
            ObjectDisposedException.ThrowIf(Disposed, this);
            Disposed = true;
            ArrayPool<byte>.Shared.Return(Buffer);
        }
    }

    public override string ToString() => $"[{nameof(VTSSocket)}]";
}
