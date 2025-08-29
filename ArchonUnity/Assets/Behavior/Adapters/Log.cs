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
                //defaultAdapter.Write($"Logging adapter updated");
            }
        }


        private static ILogAdapter defaultAdapter;
        //public static ILogAdapter Default
        //{
        //    get
        //    {
        //        if (defaultAdapter == null)
        //        {
        //            defaultAdapter = AdapterFactory(Array.Empty<string>());
        //        }
        //        return defaultAdapter;
        //    }
        //}

        //public static void Write(string v)
        //{
        //    Default.Write(v);
        //}


        //public static void LogWarning(string v)
        //{
        //    Default.LogWarning(v);
        //}

        //public static void LogError(string v, Exception ex)
        //{
        //    Default.LogError(v, ex);
        //}
        //public static void LogError(string v)
        //{
        //    Default.LogError(v);
        //}
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
