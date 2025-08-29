using Assets.Behavior.Adapters;
using System;
using UnityEngine;

public static class AudioPatcher
{
    public static void Patch(AudioSource source)
    {
        using (var log = Log.New())
        {
            if (Patcher != null)
            {
                //ConsoleControl.Write($"Patching audio source {source.name}");
                Patcher(source);
            }
            else
                log.Error($"Patcher not configured. Cannot patch audio source {source.name}");
        }
    }

    public static void PatchAll(Transform transform)
    {
        using (var log = Log.New())
        {
            if (Patcher == null)
            {
                log.Error($"Patcher not configured. Cannot patch audio sources in {transform.name}");
                return;
            }
            var sources = transform.GetComponentsInChildren<AudioSource>();
            //ConsoleControl.Write($"Patching {sources.Length} audio sources found in {transform.name}");
            foreach (var source in sources)
                Patcher(source);
        }
    }

    public static Action<AudioSource> Patcher { get; set; }

}
