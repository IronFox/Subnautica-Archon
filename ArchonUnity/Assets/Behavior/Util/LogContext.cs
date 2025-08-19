using System;
using System.Collections.Generic;
using Assets.Behavior.Adapters;

namespace Behavior.Util
{
    public class LogContext : IDisposable
    {
        private static List<LogContext> Stack { get; } = new List<LogContext>();
        public string Name { get; }
        public LogContext(string name, params object[] args)
        {
            string fullName = name;
            if (args.Length > 0)
                fullName = $"{name} ({string.Join(", ",args)})";
            if (Stack.Count > 0)
                Stack[Stack.Count - 1].Write($"> {fullName}");
            else
                Log.Write($"> {fullName}");
            Name = name;
            Indentation = Stack.Count + 1;
            Stack.Add(this);
            
        }

        public int Indentation { get; }

        public void Write(string message)
        {
            Log.Write("  ".Repeat(Indentation) + message);
        }

        public void Error(string message)
        {
            Log.LogError("  ".Repeat(Indentation) + message);
        }

        public void Error(string message, Exception ex)
        {
            Log.LogError("  ".Repeat(Indentation) + message, ex);
        }

        public void Warn(string message)
        {
            Log.LogWarning("  ".Repeat(Indentation) + message);
        }


        public void Dispose()
        {
            Stack.Remove(this);
            if (Stack.Count > 0)
                Stack[Stack.Count - 1].Write($"< {Name}");
            else
                Log.Write($"< {Name}");
            
        }

    }
}