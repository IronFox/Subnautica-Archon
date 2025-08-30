using System;
using System.Collections.Generic;
using System.Linq;

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
            return AdapterFactory((false, tags));
        }
        public static ILogAdapter NewLazy(params string[] tags)
        {
            return AdapterFactory((true, tags));
        }

        private static Func<(bool ForceLazy, string[] Tags), ILogAdapter> adapterFactory
            = (p) => new UnityLogAdapter(p.Tags);
        public static Func<(bool ForceLazy, string[] Tags), ILogAdapter> AdapterFactory
        {
            get => adapterFactory;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value), "AdapterFactory cannot be null");
                }
                adapterFactory = value;
            }
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
        public void Debug(string message)
        {
            UnityEngine.Debug.Log(MakeMessage(message, Tags.Append("Debug")));
        }

        public void Error(string message, Exception exception = null)
        {
            if (exception == null)
                UnityEngine.Debug.LogError(MakeMessage(message, Tags));
            else
            {
                UnityEngine.Debug.LogError(MakeMessage(message + ": " + exception.Message, Tags));
                UnityEngine.Debug.LogError(exception.StackTrace);
            }
        }

        public void Write(string message)
        {
            UnityEngine.Debug.Log(MakeMessage(message, Tags));
        }

        public void Warn(string message)
        {
            UnityEngine.Debug.LogWarning(MakeMessage(message, Tags));
        }

        public void LogException(Exception exception)
        {
            UnityEngine.Debug.LogException(exception);
        }

        public void Dispose()
        { }
    }

    public interface ILogAdapter : IDisposable
    {
        string[] Tags { get; }
        void Write(string message);
        void Debug(string message);
        void Warn(string message);
        void Error(string message, Exception exception = null);
    }
}
