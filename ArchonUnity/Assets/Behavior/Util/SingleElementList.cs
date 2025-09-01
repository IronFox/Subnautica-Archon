using System;
using System.Collections.Generic;

namespace Assets.Behavior.Util
{
    /// <summary>
    /// Represents a read-only list that contains a single element.
    /// </summary>
    /// <remarks>This class provides a lightweight implementation of <see cref="IReadOnlyList{T}"/> for
    /// scenarios where a list containing exactly one element is required. The indexer only supports an index of
    /// <c>0</c>, and the <see cref="Count"/> property always returns <c>1</c>.</remarks>
    /// <typeparam name="T">The type of the element in the list.</typeparam>
    internal class SingleElementList<T> : IReadOnlyList<T>
    {
        private readonly T element;
        public SingleElementList(T element)
        {
            this.element = element;
        }
        public T this[int index] => index == 0 ? element : throw new ArgumentOutOfRangeException(nameof(index));
        public int Count => 1;
        public IEnumerator<T> GetEnumerator()
        {
            yield return element;
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal static class SingleElementList
    {
        public static SingleElementList<T> Create<T>(T element) => new SingleElementList<T>(element);
    }
}
