using System;
using AVS.Log;
using Behavior.Util.Log;

namespace Subnautica_Archon.Util;

public class LogContext : AbstractLogContext
{
    public LogContext(Archon archon, string name, params object[] args) : base(name, args)
    {
        Log = archon.Log;
        LogEntry();
    }
    public LogContext(LogWriter log, string name, params object[] args) : base(name, args)
    {
        Log = log;
        LogEntry();
    }

    public LogWriter Log { get; set; }


    public override void WriteMessage(string message)
        => Log.Write(MakeMessage(message));


    public void Write(string message)
        => Log.Write(MakeMessage(message));

    public void Error(string message)
        => Log.Error(MakeMessage(message));
    public void Error(string message, Exception exception)
        => Log.Error(MakeMessage(message), exception);
}