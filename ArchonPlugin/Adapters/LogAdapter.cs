using AVS.Log;
using System;

namespace Subnautica_Archon.Adapters
{
    internal class LogAdapter : Assets.Behavior.Adapters.ILogAdapter
    {
        public LogAdapter(ArchonModController amc, bool forceLazy, string[] tags)
        {
            Tags = tags;
            Writer = new SmartLog(amc, domain: "Uty", frameDelta: 3, tags: tags, forceLazy: forceLazy);
        }
        public SmartLog Writer { get; }
        public string[] Tags { get; }

        public void Debug(string message)
        {
            Writer.Debug(message);
        }

        public void Dispose()
        {
            Writer.Dispose();
        }

        public void Error(string message, Exception? exception = null)
        {
            Writer.Error(message, exception);
        }

        public void Warn(string message)
        {
            Writer.Warn(message);
        }

        public void Write(string message)
            => Writer.Write(message);
    }
}
