using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Behavior.Adapters
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
    }
    public static class Log
    {
        public static ILogAdapter New(params string[] tags)
        {
            return AdapterFactory(tags);
        }

        private static Func<string[], ILogAdapter> adapterFactory
            = (tag) => new UnityLogAdapter(tag);
        public static Func<string[], ILogAdapter> AdapterFactory
        {
            get => adapterFactory;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value), "AdapterFactory cannot be null");
                }
                adapterFactory = value;
                defaultAdapter = adapterFactory(Array.Empty<string>()); ;
                defaultAdapter.Write($"Logging adapter updated");
            }
        }


        private static ILogAdapter defaultAdapter;
        public static ILogAdapter Default
        {
            get
            {
                if (defaultAdapter == null)
                {
                    defaultAdapter = AdapterFactory(Array.Empty<string>());
                }
                return defaultAdapter;
            }
        }

        public static void Write(string v)
        {
            Default.Write(v);
        }


        public static void LogWarning(string v)
        {
            Default.LogWarning(v);
        }

        public static void LogError(string v, Exception ex)
        {
            Default.LogError(v, ex);
        }
        public static void LogError(string v)
        {
            Default.LogError(v);
        }
    }

    internal class UnityLogAdapter : ILogAdapter
    {
        public UnityLogAdapter(string[] tags)
        {
            Tags = tags;
        }

        public string[] Tags { get; }

        private string MakeMessage(string msg, IEnumerable<string> tags)
        {
            string tagLine = tags.Any() ? $"[{string.Join("] [", tags)}] " : "";
            return $"{DateTime.Now:HH:mm:ss.fff} {tagLine}: {msg}";
        }
        public void LogDebug(string message)
        {
            Debug.Log(MakeMessage(message, Tags.Append("Debug")));
        }

        public void LogError(string message, Exception exception = null)
        {
            if (exception == null)
                Debug.LogError(MakeMessage(message, Tags));
            else
            {
                Debug.LogError(MakeMessage(message + ": " + exception.Message, Tags));
                Debug.LogError(exception.StackTrace);
            }
        }

        public void Write(string message)
        {
            Debug.Log(MakeMessage(message, Tags));
        }

        public void LogWarning(string message)
        {
            Debug.LogWarning(MakeMessage(message, Tags));
        }

        public void LogException(Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    public interface ILogAdapter
    {
        string[] Tags { get; }
        void Write(string message);
        void LogDebug(string message);
        void LogWarning(string message);
        void LogError(string message, Exception exception = null);
        void LogException(Exception exception);
    }
}
