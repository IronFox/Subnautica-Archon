using AVS.Log;
using System;
using System.Linq;

namespace Subnautica_Archon.Adapters
{
    internal class LogAdapter : Assets.Behavior.Adapters.ILogAdapter
    {
        public LogAdapter(string[] tags)
        {
            Tags = tags;
            Writer = new LogWriter(
                tags: tags.Prepend("Unity").ToArray(),
                prefix: null
            );
        }
        public LogWriter Writer { get; }
        public string[] Tags { get; }

        public void LogDebug(string message)
            => Writer.Debug(message);

        public void LogError(string message, Exception? exception = null)
            => Writer.Error(message, exception);

        public void LogException(Exception exception)
            => Writer.Error("Exception caught", exception);

        public void LogWarning(string message)
            => Writer.Warn(message);

        public void Write(string message)
            => Writer.Write(message);
    }
}
