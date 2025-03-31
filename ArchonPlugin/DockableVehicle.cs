using Nautilus.Handlers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VehicleFramework;
using static VehicleUpgradeConsoleInput;


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

        public bool ShouldUnfreezeImmediately => !(Vehicle is ModVehicle);

        public void BeginDocking()
        {
            if (HasPlayer)
                AvatarInputHandler.main.gameObject.SetActive(value: false);
            Vehicle.liveMixin.shielded = true;
            Vehicle.crushDamage.enabled = false;
            if (Vehicle is ModVehicle)
                Vehicle.docked = true;  //vanilla react oddd
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
                new MethodAdapter(Player.main.currentMountedVehicle, "OnPilotModeEnd").Invoke();
                if (Player.main.currentMountedVehicle is ModVehicle v)
                {
                    Log.Write($"Player is in mod vehicle {v}. Deselecting...");
                    v.DeselectSlots();
                    v.PlayerExit();
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

            //if (Vehicle is ModVehicle mv)
            {
                Log.Write($"Trying to set module slot for {Vehicle}");
                //CraftData.
                //var module = ModVehicleUndockModule.GetPrototypeFor( mv );


                var pu = Vehicle.gameObject.GetComponent<Pickupable>();
                if (!pu)
                {
                    Log.Write($"Attaching new Pickupable");
                    pu = Vehicle.gameObject.AddComponent<Pickupable>();
                }

                //Pickupable pu = new Pickupable();
                //pu.SetTechTypeOverride(module.TechType);
                //pu.SetVisible(true);
                InventoryItem item = new InventoryItem(pu);

                CraftDataHandler.SetQuickSlotType(CraftData.GetTechType(Vehicle.gameObject), QuickSlotType.Toggleable);
                //item.SetTechType(module.TechType);
                Log.Write($"Adding new item to slot");
                foreach (var slot in Archon.slotIDs)
                    if (Archon.modules.GetItemInSlot(slot) == null)
                    {
                        Archon.modules.AddItem(slot, item, true);

                        Log.Write($"Added to slot {slot}");
                        break;
                    }
                if (Vehicle.transform.parent != Archon.Control.hangarRoot)
                {
                    Log.Error($"Docked vehicle root has changed from {Log.PathOf(Archon.Control.hangarRoot)} to {Log.PathOf(Vehicle.transform.parent)}. Reparenting");
                    Vehicle.transform.parent = Archon.Control.hangarRoot;
                }
                if (Vehicle.transform.localPosition != Archon.Control.dockedSpace.localPosition)
                {
                    Log.Error($"Docked vehicle local position has changed from {Archon.Control.dockedSpace.localPosition} to {Vehicle.transform.localPosition}. Relocating");
                    Vehicle.transform.localPosition = Archon.Control.dockedSpace.localPosition;
                    Vehicle.transform.localRotation = Archon.Control.dockedSpace.localRotation;
                }
                Log.Write($"Mod added");

            }

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


        public void PrepareUndocking()
        {
            Archon.SuspendExitLimits();
            if (Archon.IsPlayerPiloting())
                Archon.DeselectSlots();
            if (Archon.IsPlayerInside())
                Archon.PlayerExit();

            if (Vehicle is ModVehicle mv)
            {
                mv.PlayerEntry();
                mv.BeginPiloting();
            }
            else
            {
                new MethodAdapter<Player, bool, bool>(Vehicle, "EnterVehicle").Invoke(Player.main, true, true);
                new MethodAdapter(Vehicle, "OnPilotModeBegin").Invoke();
            }
            AvatarInputHandler.main.gameObject.SetActive(value: false);
            Archon.RestoreExitLimits();

            var mode = Player.main.GetType()
                    .GetField("mode", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            mode?.SetValue(Player.main, Player.Mode.LockedPiloting);


            Log.Write($"Destroying pickupable (if any)");
            GameObject.Destroy(Vehicle.GetComponent<Pickupable>());
        }



        public void UpdateWaitingForBayDoorOpen()
        {
        }

        public void BeginUndocking()
        {
        }


        public void EndUndocking()
        {
            Vehicle.liveMixin.shielded = false;
            Vehicle.crushDamage.enabled = true;
            if (Vehicle is ModVehicle)
                Vehicle.docked = false;

            //new MethodAdapter<bool>(Vehicle, "UpdateCollidersForDocking").Invoke(false);
            AvatarInputHandler.main.gameObject.SetActive(value: true);
        }

        public void OnUndockingDone()
        {
        }


        public IEnumerable<T> GetAllComponents<T>() where T : Component
        {
            return Vehicle.GetComponentsInChildren<T>()
                .Where(x => !x.transform.IsChildOf(Player.mainObject.transform));
        }


    }
}