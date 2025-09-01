using AVS.Log;
using System;
using System.Collections.Generic;

namespace Subnautica_Archon.Adapters
{
    internal class LogAdapter : Assets.Behavior.Adapters.ILogAdapter
    {
        public LogAdapter(ArchonModController amc, bool forceLazy, IReadOnlyList<string>? tags)
        {
            Tags = tags;
            Writer = new SmartLog(amc, Domain.Unity, frameDelta: 3, tags: tags, forceLazy: forceLazy);
        }
        public SmartLog Writer { get; }
        public IReadOnlyList<string>? Tags { get; }

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
