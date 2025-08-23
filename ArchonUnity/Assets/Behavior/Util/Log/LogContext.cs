using System;
using JetBrains.Annotations;

namespace Behavior.Util.Log
{
    internal class LogContext : AbstractLogContext
    {
        public LogContext(string name, [ItemCanBeNull] params object[] args) : base(name, args)
        {
            LogEntry();
        }

        public void Write(string message)
        {
            Assets.Behavior.Adapters.Log.Write(MakeMessage(message));
        }

        public void Error(string message)
        {
            Assets.Behavior.Adapters.Log.LogError(MakeMessage(message));
        }

        public void Error(string message, Exception ex)
        {
            Assets.Behavior.Adapters.Log.LogError(MakeMessage(message), ex);
        }

        public void Warn(string message)
        {
            Assets.Behavior.Adapters.Log.LogWarning(MakeMessage(message));
        }


        public override void WriteMessage(string message)
            => Write(message);
    }
}