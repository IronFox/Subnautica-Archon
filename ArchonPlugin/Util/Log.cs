﻿using System.Collections.Generic;
using System;
using UnityEngine;
using System.IO;

namespace Subnautica_Archon.Util
{

    public static class Log
    {
        private static readonly Logging log = new Logging(true, null, true, true);
        public static string PathOf(Transform t)
        {
            if (!t)
                return "<null>";
            var parts = new List<string>();
            try
            {
                while (t)
                {
                    parts.Add($"{t.name}[{t.GetInstanceID()}]");
                    t = t.parent;
                }
            }
            catch (UnityException)  //odd, but okay, don't care
            { }
            parts.Reverse();
            return string.Join("/", parts);

        }
        public static string PathOf(Component c)
        {
            try
            {
                return PathOf(c.transform) + $":{c.name}[{c.GetInstanceID()}]({c.GetType()})";
            }
            catch (Exception)
            {
                try
                {
                    return c.name;
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }
        }
        public static void Write(string message)
        {
            log.LogMessage(message);
        }
        public static void Error(string message)
        {
            log.LogError(message);
        }

        public static void Write(Exception ex)
        {
            Debug.LogException(ex);
            //Write(ex.GetType().Name);
            //Write(ex.Message);
            //Write(ex.StackTrace);
        }
        public static void Write(string whileDoing, Exception caughtException)
        {
            log.LogError($"Caught exception during {whileDoing}");
            Write(caughtException);
        }

        public static string GetVehicleName(Vehicle v) => v.subName ? v.subName.GetName() : v.vehicleName;

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
            Log.Write(Log.PathOf(Owner) + $": {msg}");
        }
        public void Error(string msg)
        {
            Log.Error(Log.PathOf(Owner) + $": {msg}");
        }
    }

}