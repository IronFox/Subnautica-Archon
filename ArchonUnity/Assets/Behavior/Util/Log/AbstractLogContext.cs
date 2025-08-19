using System;
using System.Collections.Generic;

namespace Behavior.Util.Log
{
    public abstract class AbstractLogContext : IDisposable
    {
        private static List<AbstractLogContext> Stack { get; } = new List<AbstractLogContext>();
        public string Name { get; }
        protected AbstractLogContext(string name, params object[] args)
        {
            FullName = name;
            if (args.Length > 0)
                FullName = $"{name} ({string.Join(", ",args)})";
            Name = name;
            Indentation = Stack.Count + 1;
            Stack.Add(this);
        }

        public string FullName { get; set; }

        protected void LogEntry()
        {
            Indentation--;
            WriteMessage($"> {FullName}");
            Indentation++;
        }

        public abstract void WriteMessage(string message);

        public int Indentation { get; private set; }

        protected string MakeMessage(string message)
            => "  ".Repeat(Indentation).Append(message).ToString();


        public void Dispose()
        {
            Stack.Remove(this);
            Indentation--;
            WriteMessage($"< {Name}");
        }

    }
}