using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;

namespace Subnautica_Archon.Util
{
    /// <summary>
    /// Logging configuration
    /// </summary>
    public readonly struct Logging
    {
        public bool LogMaterialChanges { get; }
        public string Prefix { get; }
        public bool IncludeTimestamp { get; }
        public bool LogExtraSteps { get; }

        public Logging(bool logMaterialChanges, string prefix, bool includeTimestamp, bool logExtraSteps)
        {
            LogMaterialChanges = logMaterialChanges;
            Prefix = prefix;
            IncludeTimestamp = includeTimestamp;
            LogExtraSteps = logExtraSteps;
        }

        public const string DefaultPrefix = "Material Fix";

        public static Logging Default { get; } = new Logging(
            logMaterialChanges: false,
            prefix: DefaultPrefix,
            includeTimestamp: true,
            logExtraSteps: true
            );

        public static Logging Silent { get; } = new Logging(
            logMaterialChanges: false,
            prefix: DefaultPrefix,
            includeTimestamp: true,
            logExtraSteps: false
            );

        public static Logging Verbose { get; } = new Logging(
            logMaterialChanges: true,
            prefix: DefaultPrefix,
            includeTimestamp: true,
            logExtraSteps: true
            );

        public void LogExtraStep(string msg)
        {
            if (!LogExtraSteps)
                return;

            Debug.Log(MakeMessage(msg));
        }

        private string MakeMessage(string msg)
        {
            if (!string.IsNullOrEmpty(Prefix))
            {
                if (IncludeTimestamp)
                    return $"{DateTime.Now:HH:mm:ss.fff} [Archon] {Prefix}: {msg}";
                return $"{Prefix}: {msg}";
            }
            else
            {
                if (IncludeTimestamp)
                    return $"{DateTime.Now:HH:mm:ss.fff} [Archon] {msg}";
                return msg;
            }
        }


        public void LogMessage(string msg)
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

        public void LogMaterialChange(string msg)
        {
            if (!LogMaterialChanges)
                return;
            Debug.Log(MakeMessage(msg));
        }
        public void LogMaterialChange(Func<string> msg)
        {
            if (!LogMaterialChanges)
                return;
            Debug.Log(MakeMessage(msg()));
        }

        private string ValueToString<T>(T value)
        {
            if (value is float f0)
                return f0.ToString(CultureInfo.InvariantCulture);
            return value?.ToString();
        }

        public void LogMaterialVariableSet<T>(
            ShaderPropertyType type,
            string name,
            T old,
            T value,
            Material m)
        {
            if (LogMaterialChanges)
                Debug.Log(MakeMessage($"Setting {type} {name} ({ValueToString(old)} -> {ValueToString(value)}) on material {m.NiceName()}"));
        }
    }

}
