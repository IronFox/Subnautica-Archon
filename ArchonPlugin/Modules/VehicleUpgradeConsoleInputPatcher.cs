
using HarmonyLib;
using System.Collections;
using UnityEngine;


namespace Subnautica_Archon
{

    [HarmonyPatch(typeof(VehicleUpgradeConsoleInput))]
    class VehicleUpgradeConsoleInputPatcher
    {
        const float openDuration = 0.5f;
        static float timeUntilClose = 0f;
        static Coroutine closeDoorCor = null;
        public static IEnumerator closeDoorSoon(ArchonControl control)
        {
            while (timeUntilClose > 0)
            {
                timeUntilClose -= Time.deltaTime;
                yield return null;
            }
            if (control == null)
            {
                Debug.LogWarning($"Control is null in {nameof(closeDoorSoon)}");
            }
            else
            {
                Debug.LogWarning($"Timeout passed. Closing");
                control.openUpgradeCover = false;
            }
            closeDoorCor = null;
            yield break;
        }
        [HarmonyPostfix]
        [HarmonyPatch(nameof(VehicleUpgradeConsoleInput.OnHandHover))]
        public static void VehicleUpgradeConsoleInputOnHandHoverPostfix(VehicleUpgradeConsoleInput __instance, Sequence ___sequence)
        {
            //Debug.Log("VehicleUpgradeConsoleInputOnHandHoverPostfix");
            // control opening the modules hatch
            if (__instance.GetComponentInParent<Archon>() != null)
            {
                __instance.GetComponentInParent<ArchonControl>().openUpgradeCover = true;
                timeUntilClose = openDuration;
                if (closeDoorCor == null)
                {
                    closeDoorCor = UWE.CoroutineHost.StartCoroutine(closeDoorSoon(__instance.GetComponentInParent<ArchonControl>()));
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("OpenPDA")]
        public static void VehicleUpgradeConsoleInputOpenPDAPostfix(VehicleUpgradeConsoleInput __instance, Sequence ___sequence)
        {
            //Debug.Log("VehicleUpgradeConsoleInputOpenPDAPostfix");
            // control opening the modules hatch
            if (__instance.GetComponentInParent<ArchonControl>() != null)
            {
                UWE.CoroutineHost.StopCoroutine(closeDoorCor);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnClosePDA")]
        public static void VehicleUpgradeConsoleInputOnClosePDAPostfix(VehicleUpgradeConsoleInput __instance, Sequence ___sequence)
        {
            //Debug.Log("VehicleUpgradeConsoleInputOnClosePDAPostfix");
            // control opening the modules hatch
            if (__instance.GetComponentInParent<ArchonControl>() != null)
            {
                closeDoorCor = UWE.CoroutineHost.StartCoroutine(closeDoorSoon(__instance.GetComponentInParent<ArchonControl>()));
            }
        }
    }
}