using System.Globalization;
using System;

using UnityEngine;

/// <summary>
/// Logging configuration
/// </summary>
public readonly struct LogConfig
{
    public string Prefix { get; }
    public bool IncludeTimestamp { get; }

    public LogConfig(string prefix, bool includeTimestamp)
    {
        Prefix = prefix;
        IncludeTimestamp = includeTimestamp;
    }

    public const string DefaultPrefix = "Material Fix";

    public static LogConfig Default { get; } = new LogConfig(
        prefix: DefaultPrefix,
        includeTimestamp: true
        );

    public static LogConfig Silent { get; } = new LogConfig(
        prefix: DefaultPrefix,
        includeTimestamp: true
        );

    public static LogConfig Verbose { get; } = new LogConfig(
        prefix: DefaultPrefix,
        includeTimestamp: true
        );


    private string MakeMessage(string msg)
    {
        if (!string.IsNullOrEmpty(Prefix))
        {
            if (IncludeTimestamp)
                return $"{DateTime.Now:HH:mm:ss.fff} {Prefix}: {msg}";
            return $"{Prefix}: {msg}";
        }
        else
        {
            if (IncludeTimestamp)
                return $"{DateTime.Now:HH:mm:ss.fff} {msg}";
            return msg;
        }
    }


    public void Write(string msg)
    {
        Debug.Log(MakeMessage(msg));
    }
    
    public void LogWarning(string msg)
    {
        Debug.LogWarning(MakeMessage(msg));
    }

    public void LogError(string msg)
    {
        Debug.LogError(MakeMessage(msg));
    }

    public void LogException(Exception ex)
    {
        LogError(ex.ToString());
        Debug.LogException(ex);
    }

    public void LogException(string context, Exception ex)
    {
        LogError(context+": "+ex);
        Debug.LogException(ex);
    }


    private string ValueToString<T>(T value)
    {
        if (value is float f0)
            return f0.ToString(CultureInfo.InvariantCulture);
        return value?.ToString();
    }

}

