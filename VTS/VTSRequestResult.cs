using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using VoiceTrigger.VTS.Packets;

namespace VoiceTrigger.VTS;

public readonly struct VTSRequestResult
{
    // This is preferred, since it allows to not write "<T>" on each usage..
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VTSRequestResult<T> FromResult<T>(T? data) where T : VTSResponseTemplate => VTSRequestResult<T>.FromResult(data);
}

// TODO: Also return a reason of failure/success.
public readonly struct VTSRequestResult<T> where T : VTSResponseTemplate
{
    public static readonly VTSRequestResult<T> Failed = FromResult(null);
    public static VTSRequestResult<T> FromResult(T? response) => new(response);
    private VTSRequestResult(T? response)
    {
        Success = response is not null;
        Response = response;
    }

    [MemberNotNullWhen(true, nameof(Response))]
    public bool Success { get; init; }
    public T? Response { get; init; }
    public bool ResolveSuccess([NotNullWhen(true)] out T? response)
    {
        response = Response;
        return Success;
    }

    public override string ToString() => $"Request Result: {(Success ? "Success" : "Failure")}, Response: {Response}";
}