using System;

public enum TranslationCode
{
    Modules,
    PowerCells,
    NothingDocked
}


public static class TranslationAdapter
{
    public static Func<TranslationCode, string> GetTranslation { get; set; } = (code) =>
    {
        switch (code)
        {
            case TranslationCode.Modules:
                return "Modules";
            case TranslationCode.PowerCells:
                return "Power Cells";
            case TranslationCode.NothingDocked:
                return "Nothing Docked";
            default:
                throw new ArgumentOutOfRangeException(nameof(code), code, null);
        }
    };
}
