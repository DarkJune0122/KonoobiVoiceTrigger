using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;
using VoiceTrigger.Logging;

namespace VoiceTrigger.VTS;

public sealed partial class ConcurrentTokenStorage(string directory, string fileName) : ObservableObject, ITokenStorage
{
    /// <summary>
    /// Directory to create, at which token will be stored.
    /// </summary>
    readonly string FileDirectory = directory;
    /// <summary>
    /// Path where file exists.
    /// </summary>
    readonly string FilePath = Path.Combine(directory, fileName);

    // Slim is used, in oder to allow using Tasks from a ThreadPool.
    // Also, because we don't expect it to be used all that frequently.
    readonly SemaphoreSlim Semaphore = new(0, 1);
    string? Token; // Cached instead of reading from file each time, as we don't expect user to overwrite the token manually.

    public async ValueTask<string?> GetToken()
    {
        await Semaphore.WaitAsync();
        try
        {
            if (Token is not null)
                return Token;

            if (!File.Exists(FilePath))
                return null;

            string token = await File.ReadAllTextAsync(FilePath);
            if (string.IsNullOrWhiteSpace(token))
                return null;

            Token = token;
            return token;
        }
        catch (Exception ex) { ex.Out(ToString()); }
        finally { Semaphore.Release(); }
        return null;
    }

    public async ValueTask SetToken(string token)
    {
        if (token is null) return;
        await Semaphore.WaitAsync();
        try
        {
            Token = token; // Updates cached token as well.

            Directory.CreateDirectory(FileDirectory);
            await File.WriteAllTextAsync(FilePath, token);
        }
        catch (Exception ex) { ex.Out(ToString()); }
        finally { Semaphore.Release(); }
    }

    public async ValueTask DeleteToken()
    {
        await Semaphore.WaitAsync();
        try
        {
            Token = null; // Also invalidates cached token before any IOExceptions can occur.

            // Skipping File.Exist to avoid race conditions.
            // As a consequence, we need to explicitly handle FileNotFoundException.
            File.Delete(FilePath);
        }
        catch (FileNotFoundException) { /* Ignored */ }
        catch (Exception ex) { ex.Out(ToString()); }
        finally { Semaphore.Release(); }
    }

    public override string ToString() => $"[{nameof(ConcurrentTokenStorage)}]";
}
