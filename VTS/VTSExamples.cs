using VoiceTrigger.VTS.Requests;

namespace VoiceTrigger.VTS;

public static class VTSExamples
{
    static readonly VTSConnection VTS = new(new VTSEndPoint()
    {
        InstanceID = "test",
        Active = true,
        Port = 8001,
        WindowTitle = "Debug VTS Instance"
    });
    public static void Main()
    {
        throw new NotImplementedException();
    }
    public static async void MainAsync()
    {
        var state = await VTS.RequestAPIState();
        if (state.Succeeded)
        {

        }
        else
        {

        }
    }
    public static void Stack()
    {
        Pack([], out var response);


        throw new NotImplementedException();
    }

    public static void Pack(ReadOnlySpan<byte> span, out VTSStackAPIStateResponse response)
    {
        response = default;
    }

    public static async void StackAsync()
    {
        await Task.CompletedTask;
        throw new NotImplementedException();
    }

    public static ValueTask<ReadOnlySpan<byte>> RequestJson(object input)
    {
        // Do some work with VTS instance.
        

        return 
    }
}

public readonly ref struct JsonResult(bool success, ReadOnlySpan<byte> json)
{
    public readonly bool Success = success;
    public readonly ReadOnlySpan<byte> Json = json;
}

public static class Extensions
{
    public static VTSStackAPIStateResponse AsAPIState(this JsonResult result)
    {
        if (result.Success)
        {
            return new(result.Json);
        }
        else
        {
            return new();
        }
    }
}