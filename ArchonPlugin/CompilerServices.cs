namespace System.Runtime.CompilerServices
{
    // This class is a marker type required for init-only setters in C# 9.
    // It's typically included in .NET 5.0 and later.
    // For older target frameworks, it needs to be manually defined.
    internal static class IsExternalInit { }
}