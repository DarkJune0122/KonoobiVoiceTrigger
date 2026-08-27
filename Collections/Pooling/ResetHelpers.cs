namespace VoiceTrigger.Collections.Pooling;

/// <summary>
/// Helper methods for resetting common known types.
/// </summary>
public sealed class ResetHelpers
{
    public static void ResetArray<T>(T[] array) => Array.Clear(array);
    public static void ResetList<T>(List<T> list) => list.Clear();
}

//public sealed class ListPool<T> : Pool<List<T>>
//{
//    public override List<T> Rent(Func<List<T>> ctor)
//    {
//        throw new NotImplementedException();
//    }

//    public override void Return(List<T> value)
//    {
//        throw new NotImplementedException();
//    }

//    public override void Return(List<T> value, Action<List<T>> reset)
//    {
//        throw new NotImplementedException();
//    }
//}