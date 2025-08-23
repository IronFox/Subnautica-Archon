using System.Diagnostics.CodeAnalysis;

namespace Subnautica_Archon.Util;

public static class IsNullExtensions
{
    public static bool IsNull([NotNullWhen(false)] this IDockable? dockable)
        => dockable is null;
    public static bool IsNotNull([NotNullWhen(true)] this IDockable? dockable)
        => dockable is not null;
}