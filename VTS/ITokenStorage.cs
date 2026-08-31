namespace VoiceTrigger.VTS;

public interface ITokenStorage
{
    /// <returns>
    /// A non-null string when token is found.
    /// <see langword="null"/> if token is invalidated, or cannot be loaded.
    /// </returns>
    public ValueTask<string?> GetToken();
    /// <remarks>
    /// When <paramref name="token"/> is set to <see langword="null"/> - it will ignore it completely.
    /// Task will return immediately as well.
    /// </remarks>
    public ValueTask SetToken(string token);
    /// <summary>
    /// Deletes token from the cache and underlying file in a system.
    /// </summary>
    /// <returns></returns>
    public ValueTask DeleteToken();
}
