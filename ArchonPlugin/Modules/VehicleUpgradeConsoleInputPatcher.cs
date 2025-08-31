using AVS.Log;
using AVS.Util;
using HarmonyLib;
using Subnautica_Archon.Util;
using System.Collections;
using UnityEngine;


namespace Subnautica_Archon
{

    [HarmonyPatch(typeof(VehicleUpgradeConsoleInput))]
    class VehicleUpgradeConsoleInputPatcher
    {
        const float openDuration = 0.5f;
        static float timeUntilClose = 0f;
        static Coroutine? closeDoorCor = null;
        public static IEnumerator closeDoorSoon(SmartLog log, Archon archon)
        {
            while (timeUntilClose > 0)
            {
                timeUntilClose -= Time.deltaTime;
                yield return null;
            }
            log.Warn($"VehicleUpgradeConsoleInputPatcher.closeDoorSoon: Timeout passed. Closing");
            archon.Control.openUpgradeCover = false;
            closeDoorCor = null;
        }
        [HarmonyPostfix]
        [HarmonyPatch(nameof(VehicleUpgradeConsoleInput.OnHandHover))]
        public static void VehicleUpgradeConsoleInputOnHandHoverPostfix(VehicleUpgradeConsoleInput __instance, Sequence ___sequence)
        {
            //Debug.Log("VehicleUpgradeConsoleInputOnHandHoverPostfix");
            // control opening the modules hatch
            var arc = __instance.SafeGetComponentInParent<Archon>();
            if (arc.IsNotNull() && arc.upgradesInput == __instance)
            {
                //Log.Write($"VehicleUpgradeConsoleInputPatcher.VehicleUpgradeConsoleInputOnHandHoverPostfix: {__instance.GetComponentInParent<ArchonControl>().NiceName()}");
                arc.Control.openUpgradeCover = true;
                timeUntilClose = openDuration;
                if (closeDoorCor.IsNull())
                {
                    closeDoorCor = arc.Owner.StartModCoroutine(nameof(VehicleUpgradeConsoleInputPatcher) + '.' + nameof(closeDoorSoon), log => closeDoorSoon(log, arc));
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("OpenPDA")]
        public static void VehicleUpgradeConsoleInputOpenPDAPostfix(VehicleUpgradeConsoleInput __instance, Sequence ___sequence)
        {
            //Debug.Log("VehicleUpgradeConsoleInputOpenPDAPostfix");
            // control opening the modules hatch
            var arc = __instance.SafeGetComponentInParent<Archon>();
            if (arc.IsNotNull() && arc.upgradesInput == __instance)
            {
                //Log.Write($"VehicleUpgradeConsoleInputPatcher.VehicleUpgradeConsoleInputOpenPDAPostfix: {__instance.GetComponentInParent<ArchonControl>().NiceName()}");
                if (closeDoorCor.IsNotNull())
                    arc.Owner.StopCoroutine(closeDoorCor);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnClosePDA")]
        public static void VehicleUpgradeConsoleInputOnClosePDAPostfix(VehicleUpgradeConsoleInput __instance, Sequence ___sequence)
        {
            //Debug.Log("VehicleUpgradeConsoleInputOnClosePDAPostfix");
            // control opening the modules hatch
            var arc = __instance.SafeGetComponentInParent<Archon>();
            if (arc.IsNotNull() && arc.upgradesInput == __instance)
            {
                //Log.Write($"VehicleUpgradeConsoleInputPatcher.VehicleUpgradeConsoleInputOnClosePDAPostfix: {__instance.GetComponentInParent<ArchonControl>().NiceName()}");
                closeDoorCor = arc.Owner.StartModCoroutine(nameof(VehicleUpgradeConsoleInputPatcher) + '.' + nameof(closeDoorSoon), log => closeDoorSoon(log, arc));
            }
        }
    }
}