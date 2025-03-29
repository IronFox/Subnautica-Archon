using System.Collections.Generic;

public static class ListExtensions
{
    public static void AddRange<T>(this HashSet<T> set, IEnumerable<T> items)
    {
        foreach (var item in items)
            set.Add(item);
    }

}