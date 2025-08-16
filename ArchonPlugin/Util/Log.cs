using AVS.Log;
using System;
using UnityEngine;

namespace Subnautica_Archon.Util
{

    public static class Log
    {
        internal static LogWriter Writer { get; } = new LogWriter(
            prefix: null, "Mod");


        public static void Write(string message)
        {
            Writer.Write(message);
        }
        public static void Warn(string message)
        {
            Writer.Warn(message);
        }
        public static void Error(string message)
        {
            Writer.Error(message);
        }
        public static void Debug(string message)
            => Writer.Debug(message);

        public static void Error(string prefix, Exception ex)
            => Exception(prefix, ex);
        public static void Exception(string prefix, Exception ex)
        {
            Writer.Error(prefix, ex);
        }

        public static void Write(Exception ex)
        {
            Writer.Error($"Exception caught", ex);
            //Write(ex.GetType().Name);
            //Write(ex.Message);
            //Write(ex.StackTrace);
        }
        public static void Write(string whileDoing, Exception caughtException)
            => Writer.Error(whileDoing, caughtException);

        internal static string Describe(Vehicle vehicle)
        {
            if (!vehicle)
                return "<null vehicle>";

            return vehicle.NiceName();
        }
    }



    public class MyLogger
    {
        public Component Owner { get; }

        public enum Channel
        {
            One,
            Two,
            Three,
            Four,
            Five,
            Six,

            Count
        }

        private DateTime[] LastStamp { get; } = new DateTime[(int)Channel.Count];

        public MyLogger(Component owner)
        {
            Owner = owner;
            for (int i = 0; i < LastStamp.Length; i++)
                LastStamp[i] = DateTime.MinValue;
        }

        public void WriteLowFrequency(Channel channel, string msg)
        {
            DateTime now = DateTime.Now;
            if (now - LastStamp[(int)channel] < TimeSpan.FromMilliseconds(1000))
                return;
            LastStamp[(int)channel] = now;
            Write(msg);
        }
        public void Write(string msg)
        {
            Log.Write(Owner.GetPath() + $": {msg}");
        }
        public void Error(string msg)
        {
            Log.Error(Owner.GetPath() + $": {msg}");
        }
    }

}