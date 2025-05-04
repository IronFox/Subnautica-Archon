using System;
using System.Collections.Generic;

public static class ListExtensions
{
    public static void AddRange<T>(this HashSet<T> set, IEnumerable<T> items)
    {
        foreach (var item in items)
            set.Add(item);
    }

    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source)
            action(item);
    }
    public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
    {
        return new HashSet<T>(source);
    }

    public static T Least<T>(this IEnumerable<T> source, Func<T, float> selector)
    {
        var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext())
            throw new InvalidOperationException("Sequence contains no elements");
        var min = enumerator.Current;
        var minValue = selector(min);
        while (enumerator.MoveNext())
        {
            var value = selector(enumerator.Current);
            if (value < minValue)
            {
                minValue = value;
                min = enumerator.Current;
            }
        }
        return min;
    }

    public static T LeastOrDefault<T>(this IEnumerable<T> source, Func<T, float> selector)
    {
        var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext())
            return default;
        var min = enumerator.Current;
        var minValue = selector(min);
        while (enumerator.MoveNext())
        {
            var value = selector(enumerator.Current);
            if (value < minValue)
            {
                minValue = value;
                min = enumerator.Current;
            }
        }
        return min;
    }

}