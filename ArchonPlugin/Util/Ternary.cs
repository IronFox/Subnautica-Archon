namespace Subnautica_Archon.Util;

public struct Ternary<T> where T : class
{
    public T? Item { get; private set; }
    public bool IsSet { get; private set; }
    public bool HasFailed { get; set; }

    public bool IsSetNotFailed => IsSet && Item is not null && !HasFailed;


    public void Set(T? item)
    {
        Item = item;
        IsSet = true;
    }
}

public struct ValueTernary<T> where T : struct
{
    public T? Item { get; private set; }
    public bool IsSet { get; private set; }
    public bool HasFailed { get; set; }

    public bool IsSetNotFailed => IsSet && Item is not null && !HasFailed;


    public void Set(T? item)
    {
        Item = item;
        IsSet = true;
    }
}