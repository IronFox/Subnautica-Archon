using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VehicleFramework;


namespace Subnautica_Archon
{

    public class DockableVehicle : IDockable
    {
        public DockableVehicle(Vehicle vehicle, Archon archon)
        {
            Vehicle = vehicle;
            Archon = archon;
            HasPlayer = Player.main.currentMountedVehicle == Vehicle;
        }
        //private LogConfig Log { get; } = new LogConfig(false,"Dockable",true,true);
        public Vehicle Vehicle { get; }
        public Archon Archon { get; }
        public bool HasPlayer { get; }
        private Transform FixParentTo { get; set; }

        public GameObject GameObject => Vehicle.gameObject;
        private int UpdateCounter { get; set; }

        public void BeginDocking()
        {
            if (HasPlayer)
                AvatarInputHandler.main.gameObject.SetActive(value: false);
            Vehicle.liveMixin.shielded = true;
            Vehicle.crushDamage.enabled = false;
        }


        private void MovePlayerToArchon()
        {
            Log.Write("(Re-)Switching player to archon");

            var mode = Player.main.GetType()
                    .GetField("mode", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (mode == null)
            {
                Log.Error($"Unable to find mode field on Player.main");
            }
            if (Player.main.currentMountedVehicle)
            {
                switch (Player.main.currentMountedVehicle)
                {
                    case ModVehicle v:
                        {
                            Log.Write($"Player is in mod vehicle {v}. Deselecting...");
                            v.DeselectSlots();
                            v.PlayerExit();
                        }
                        break;
                    case Vehicle s:
                        {
                            Log.Write($"Player is in non-mod vehicle {Player.main.currentMountedVehicle}. Ending pilot mode...");
                            new MethodAdapter(s, "OnPilotModeEnd").Invoke();
                        }
                        break;
                }
            }


            Player.main.ToNormalMode(findNewPosition: false);
            Log.Write("Zeroing velocity");
            Player.main.rigidBody.angularVelocity = Vector3.zero;
            Log.Write("Exiting locked mode");
            Player.main.ExitLockedMode(respawn: false, findNewPosition: false);
            Player.main.SetPosition(Archon.PilotSeats.First().ExitLocation.position);
            Log.Write("Exiting sitting mode");
            Player.main.ExitSittingMode();


            Log.Write($"Cleaning up");
            {
                GameInput.ClearInput();
                Player.main.transform.parent = null;
                Player.main.transform.localScale = Vector3.one;
                Player.main.currentMountedVehicle = null;
                Player.main.playerController.SetEnabled(enabled: true);
                mode?.SetValue(Player.main, Player.Mode.Normal);
                //Player.main.mode = Player.Mode.Normal;
                Player.main.playerModeChanged?.Trigger(Player.Mode.Normal);
                Player.main.sitting = false;
                Player.main.playerController.ForceControllerSize();
            }
            Log.Write($"Entering archon from transform parent {Player.main.transform.parent}");
            Archon.EnterFromDocking();
            FixParentTo = Player.main.transform.parent;
            UpdateCounter = 0;
            Log.Write($"Player transform parent now {Log.PathOf(Player.main.transform.parent)}");
            Log.Write($"Player vehicle now {Player.main.GetVehicle()} / {Log.PathOf(Player.main.GetVehicle().transform)}");
            Log.Write($"A-Okay = {VehicleFramework.Admin.Utils.IsAnAncestorTheCurrentMountedVehicle(Player.main.transform)}");
            AvatarInputHandler.main.gameObject.SetActive(value: true);
        }

        
        public void EndDocking()
        {
            if (HasPlayer)
            {

                MovePlayerToArchon();

            }
        }
        public void OnDockingDone()
        {
            Log.Write("Docking done");
            if (HasPlayer)
            {
                Log.Write($"Player transform parent now {Log.PathOf(Player.main.transform.parent)}");
                Log.Write($"Player vehicle now {Player.main.GetVehicle()} / {Log.PathOf(Player.main.GetVehicle().transform)}");
                Log.Write($"A-Okay = {VehicleFramework.Admin.Utils.IsAnAncestorTheCurrentMountedVehicle(Player.main.transform)}");
            }
        }

        public void UpdateWaitingForBayDoorClose()
        {
            UpdateCounter++;
            if (HasPlayer)
            {
                if (!VehicleFramework.Admin.Utils.IsAnAncestorTheCurrentMountedVehicle(Player.main.transform))
                {
                    Log.Error($"Player ancencestry broken at update #{UpdateCounter}");
                    if (FixParentTo)
                    {
                        MovePlayerToArchon();
                        //Player.main.transform.parent = FixParentTo;

                        if (VehicleFramework.Admin.Utils.IsAnAncestorTheCurrentMountedVehicle(Player.main.transform))
                        {
                            Log.Write($"Fixed to {Log.PathOf(FixParentTo)}");
                        }
                        else
                        {
                            Log.Error($"Fix failed (tried {Log.PathOf(FixParentTo)})");
                            FixParentTo = null;
                        }
                    }
                    else
                        Log.Error($"Cannot fix. No correction target memorized");
                }
            }
        }


        public void BeginUndocking()
        {
            if (Archon.IsPlayerPiloting())
                Archon.DeselectSlots();
            if (Archon.IsPlayerInside())
                Archon.PlayerExit();
            if (Vehicle is ModVehicle mv)
                mv.BeginPiloting();
            else
                new MethodAdapter<Player, bool, bool>(Vehicle, "EnterVehicle").Invoke(Player.main, true, true);
            AvatarInputHandler.main.gameObject.SetActive(value: false);
        }


        public void EndUndocking()
        {
            Vehicle.liveMixin.shielded = false;
            Vehicle.crushDamage.enabled = true;
            //new MethodAdapter<bool>(Vehicle, "UpdateCollidersForDocking").Invoke(false);
            AvatarInputHandler.main.gameObject.SetActive(value: true);
        }

        public IEnumerable<T> GetAllComponents<T>() where T : Component
        {
            return Vehicle.GetComponentsInChildren<T>()
                .Where(x => !x.transform.IsChildOf(Player.mainObject.transform));
        }


    }
}