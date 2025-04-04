using Nautilus.Handlers;
using Subnautica_Archon.Util;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VehicleFramework;
using VehicleFramework.VehicleTypes;
using static VehicleUpgradeConsoleInput;


namespace Subnautica_Archon.Adapters
{

    public class DockableVehicle : IDockable
    {
        private FieldAdapter<Player.Mode> Mode { get; }
        public DockableVehicle(Vehicle vehicle, Archon archon)
        {
            Vehicle = vehicle;
            Archon = archon;
            HasPlayer = Player.main.currentMountedVehicle == Vehicle && !(Vehicle is Drone);
            IsPlayerControlledDrone = Vehicle is Drone d && d.IsPlayerControlling();
            Mode = FieldAdapter.OfNonPublic<Player.Mode>(Player.main, "mode");
        }
        //private Logging Log { get; } = new Logging(false,"Dockable",true,true);
        public Vehicle Vehicle { get; }
        public Archon Archon { get; }
        public bool HasPlayer { get; }
        public bool IsPlayerControlledDrone { get; }
        private Transform FixParentTo { get; set; }

        public GameObject GameObject => Vehicle.gameObject;
        private int UpdateCounter { get; set; }

        public bool ShouldUnfreezeImmediately => !(Vehicle is ModVehicle);

        public bool UndockUpright => true;


        public override string ToString()
            => $"<Adapter>"+Log.GetVehicleName(Vehicle);

        private Bounds? bounds;
        public Bounds LocalBounds
        {
            get
            {
                if (bounds is null)
                    bounds = Vehicle.transform.ComputeScaledLocalBounds(includeRenderers: false, includeColliders: true);
                return bounds.Value;
            }
        }



        public void RestoreDockedStateFromSaveGame()
        {
            Vehicle.liveMixin.shielded = true;
            Vehicle.crushDamage.enabled = false;
            //if (Vehicle is ModVehicle)
                Vehicle.docked = true;  //vanilla vehicles react oddly
            if (Vehicle is Drone d)
                d.isAsleep = true;
            EndDocking();

        }



        public void BeginDocking()
        {
            if (HasPlayer)
            {
                Helper.ChangeAvatarInput(false);
            }
            else if (Vehicle is Drone d)
            {
                if (IsPlayerControlledDrone)
                {
                    Log.Write($"Stopping drone control");
                    d.StopControlling();

                    Helper.ChangeAvatarInput(true);
                    if (!Player.main.ToNormalMode(false) && Mode != Player.Mode.Normal)
                    {
                        Log.Write($"ToNormalMode() refused and mode is not normal. Forcing to normal");
                        Mode.Set(Player.Mode.Normal);
                    }
                    Player.main.playerController.SetEnabled(true);
                    Player.main.playerController.ForceControllerSize();
                }
                d.isAsleep = true;
            }

            Vehicle.liveMixin.shielded = true;
            Vehicle.crushDamage.enabled = false;
            if (Vehicle is ModVehicle mv)
            {
                mv.pingInstance.SetHudIcon(false);
            }
            else
                Vehicle.subName.pingInstance.SetHudIcon(false);

            if (Vehicle is ModVehicle || !HasPlayer)    //otherwise the hands are all wrong
                Vehicle.docked = true;
        }


        private void MovePlayerToArchon()
        {
            Log.Write("(Re-)Switching player to archon");

            if (HasPlayer)
            {
                new MethodAdapter(Vehicle, "OnPilotModeEnd").Invoke();
                if (Vehicle is ModVehicle v)
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
                Mode.Set(Player.Mode.Normal);
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
            Helper.ChangeAvatarInput(true);
        }


        public void EndDocking()
        {

            //if (Vehicle is ModVehicle mv)
            {
                Log.Write($"Trying to set module slot for {Vehicle}");
                //CraftData.
                //var module = ModVehicleUndockModule.GetPrototypeFor( mv );

                Vehicle.docked = true;

                var pu = Vehicle.gameObject.GetComponent<Pickupable>();
                if (!pu)
                {
                    Log.Write($"Attaching new Pickupable");
                    pu = Vehicle.gameObject.AddComponent<Pickupable>();
                }

                //Pickupable pu = new Pickupable();
                //pu.SetTechTypeOverride(module.TechType);
                //pu.SetVisible(true);

                CraftDataHandler.SetQuickSlotType(CraftData.GetTechType(Vehicle.gameObject), QuickSlotType.Toggleable);
                //item.SetTechType(module.TechType);
                bool found = false;
                foreach (var slot in Archon.QuickSlots)
                {
                    var existing = Archon.modules.GetItemInSlot(slot.ID);
                    if (existing?.item == pu)
                    {
                        found = true;
                        Log.Write($"Found {pu} in slot {slot}. Not adding but toggling off");
                        Archon.ToggleSlot(slot, false);
                        break;
                    }

                }
                if (!found)
                {
                    Log.Write($"Adding new item to slot");
                    InventoryItem item = new InventoryItem(pu);
                    QuickSlot? addedTo = null;
                    foreach (var slot in Archon.QuickSlots)
                    {
                        if (Archon.modules.GetItemInSlot(slot.ID) == null)
                        {
                            Archon.modules.AddItem(slot.ID, item, true);
                            addedTo = slot;
                            Archon.ToggleSlot(slot, false);
                            Log.Write($"Added to slot {slot}");
                            break;
                        }
                    }
                    if (addedTo.HasValue)
                    {
                        if (!HasPlayer && !IsPlayerControlledDrone)
                        {
                            Archon.SignalQuickslotsChangedWhilePiloting(addedTo.Value);
                        }
                    }
                    else
                        Log.Error($"Unable to find suitable quickslot for docked sub {pu}. Sub will not be listed in quickbar");
                }

                Log.Write($"Mod added");

            }

            if (HasPlayer)
            {

                MovePlayerToArchon();

            }
            //else if (Vehicle is Drone d)
            //{
            //    if (d.gameObject.activeSelf)
            //    {
            //        Log.Write($"Disabling drone");
            //        d.gameObject.SetActive(false);
            //    }
            //}
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
            //else if (Vehicle is Drone d)
            //{
            //    if (d.gameObject.activeSelf)
            //    {
            //        Log.Write($"Disabling drone");
            //        d.gameObject.SetActive( false );
            //    }
            //}



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

        private void SwitchToUndockingCraft()
        {

            Archon.SuspendExitLimits();
            try
            {
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
                Helper.ChangeAvatarInput(false);
                Mode.Set(Player.Mode.LockedPiloting);
            }
            finally
            {
                Archon.RestoreExitLimits();
            }
        }


        public void PrepareUndocking()
        {
            if (Vehicle is Drone d)
            {
            }
            else
            {
                if (!(Vehicle is ModVehicle))
                {
                    Vehicle.docked = false;//early unset for vanilla or hands are all wrong
                }
                if (!(Vehicle is Drone))
                    SwitchToUndockingCraft();
                //else
                //    ChangeAvatarInput(false);
            }

            Log.Write($"Destroying pickupable (if any)");
            Object.Destroy(Vehicle.GetComponent<Pickupable>());
        }



        public void UpdateWaitingForBayDoorOpen()
        {
        }

        public void BeginUndocking()
        {
            Vehicle.subName.pingInstance.SetHudIcon(true);
        }


        public void EndUndocking()
        {
            Vehicle.liveMixin.shielded = false;
            Vehicle.crushDamage.enabled = true;
            //if (Vehicle is ModVehicle)
                Vehicle.docked = false;

            if (Vehicle is Drone d)
                d.isAsleep = false;
            else
                Helper.ChangeAvatarInput(true);
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