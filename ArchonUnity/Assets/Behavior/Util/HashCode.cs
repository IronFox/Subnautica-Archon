// Polyfill for System.HashCode for older .NET/Unity runtimes.
// Safe to include in projects that target frameworks lacking System.HashCode.
// If you later target a framework that provides System.HashCode, the guard below
// will avoid type conflicts by not compiling this polyfill.

#if !NETSTANDARD2_1 && !NETCOREAPP2_1_OR_GREATER && !NET5_0_OR_GREATER && !NET6_0_OR_GREATER && !NET7_0_OR_GREATER
using System.Collections.Generic;

namespace System
{
	// Minimal, dependency-free implementation inspired by HashHelpers.Combine.
	// Supports Add and Combine overloads similar to System.HashCode.
	internal struct HashCode
	{
		private int _hash;

		// Adds a value's hash code using EqualityComparer<T>.Default.
		public void Add<T>(T value)
		{
			_hash = CombineCore(_hash, GetHashCode(value, EqualityComparer<T>.Default));
		}

		// Adds a value's hash code using a custom comparer.
		public void Add<T>(T value, IEqualityComparer<T> comparer)
		{
			if (comparer == null) comparer = EqualityComparer<T>.Default;
			_hash = CombineCore(_hash, GetHashCode(value, comparer));
		}

		// Finalizes and returns the computed hash code.
		public int ToHashCode()
		{
			return _hash;
		}

		// Static Combine helpers (1..8) mirroring System.HashCode API shape.

		public static int Combine<T1>(T1 value1)
		{
			return GetHashCode(value1, EqualityComparer<T1>.Default);
		}

		public static int Combine<T1, T2>(T1 value1, T2 value2)
		{
			int h = GetHashCode(value1, EqualityComparer<T1>.Default);
			h = CombineCore(h, GetHashCode(value2, EqualityComparer<T2>.Default));
			return h;
		}

		public static int Combine<T1, T2, T3>(T1 value1, T2 value2, T3 value3)
		{
			int h = GetHashCode(value1, EqualityComparer<T1>.Default);
			h = CombineCore(h, GetHashCode(value2, EqualityComparer<T2>.Default));
			h = CombineCore(h, GetHashCode(value3, EqualityComparer<T3>.Default));
			return h;
		}

		public static int Combine<T1, T2, T3, T4>(T1 value1, T2 value2, T3 value3, T4 value4)
		{
			int h = GetHashCode(value1, EqualityComparer<T1>.Default);
			h = CombineCore(h, GetHashCode(value2, EqualityComparer<T2>.Default));
			h = CombineCore(h, GetHashCode(value3, EqualityComparer<T3>.Default));
			h = CombineCore(h, GetHashCode(value4, EqualityComparer<T4>.Default));
			return h;
		}

		public static int Combine<T1, T2, T3, T4, T5>(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5)
		{
			int h = GetHashCode(v1, EqualityComparer<T1>.Default);
			h = CombineCore(h, GetHashCode(v2, EqualityComparer<T2>.Default));
			h = CombineCore(h, GetHashCode(v3, EqualityComparer<T3>.Default));
			h = CombineCore(h, GetHashCode(v4, EqualityComparer<T4>.Default));
			h = CombineCore(h, GetHashCode(v5, EqualityComparer<T5>.Default));
			return h;
		}

		public static int Combine<T1, T2, T3, T4, T5, T6>(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6)
		{
			int h = GetHashCode(v1, EqualityComparer<T1>.Default);
			h = CombineCore(h, GetHashCode(v2, EqualityComparer<T2>.Default));
			h = CombineCore(h, GetHashCode(v3, EqualityComparer<T3>.Default));
			h = CombineCore(h, GetHashCode(v4, EqualityComparer<T4>.Default));
			h = CombineCore(h, GetHashCode(v5, EqualityComparer<T5>.Default));
			h = CombineCore(h, GetHashCode(v6, EqualityComparer<T6>.Default));
			return h;
		}

		public static int Combine<T1, T2, T3, T4, T5, T6, T7>(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6, T7 v7)
		{
			int h = GetHashCode(v1, EqualityComparer<T1>.Default);
			h = CombineCore(h, GetHashCode(v2, EqualityComparer<T2>.Default));
			h = CombineCore(h, GetHashCode(v3, EqualityComparer<T3>.Default));
			h = CombineCore(h, GetHashCode(v4, EqualityComparer<T4>.Default));
			h = CombineCore(h, GetHashCode(v5, EqualityComparer<T5>.Default));
			h = CombineCore(h, GetHashCode(v6, EqualityComparer<T6>.Default));
			h = CombineCore(h, GetHashCode(v7, EqualityComparer<T7>.Default));
			return h;
		}

		public static int Combine<T1, T2, T3, T4, T5, T6, T7, T8>(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6, T7 v7, T8 v8)
		{
			int h = GetHashCode(v1, EqualityComparer<T1>.Default);
			h = CombineCore(h, GetHashCode(v2, EqualityComparer<T2>.Default));
			h = CombineCore(h, GetHashCode(v3, EqualityComparer<T3>.Default));
			h = CombineCore(h, GetHashCode(v4, EqualityComparer<T4>.Default));
			h = CombineCore(h, GetHashCode(v5, EqualityComparer<T5>.Default));
			h = CombineCore(h, GetHashCode(v6, EqualityComparer<T6>.Default));
			h = CombineCore(h, GetHashCode(v7, EqualityComparer<T7>.Default));
			h = CombineCore(h, GetHashCode(v8, EqualityComparer<T8>.Default));
			return h;
		}

		// Helpers

		private static int GetHashCode<T>(T value, IEqualityComparer<T> comparer)
		{
			return value == null ? 0 : comparer.GetHashCode(value);
		}

		// A fast, well-known integer hash combiner:
		// rol5(h1) + h1 XOR h2
		private static int CombineCore(int h1, int h2)
		{
			unchecked
			{
				uint rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
				return (int)(rol5 + (uint)h1) ^ h2;
			}
		}
	}
}
#endif