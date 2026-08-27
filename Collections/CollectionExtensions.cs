namespace VoiceTrigger.Collections;

public static class CollectionExtensions
{
    public static int FindIndex<T>(this IReadOnlyList<T> list, Predicate<T> predicate)
    {
        int iterator = 0;
        int length = list.Count;
        while (true)
        {
            if (iterator >= length)
                return -1;

            if (predicate(list[iterator]))
                return iterator;

            iterator++;
        }
    }
}
