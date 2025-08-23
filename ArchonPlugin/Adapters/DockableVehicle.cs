using AVS.Log;
using Subnautica_Archon.Adapters.VehicleAbstraction;
using Subnautica_Archon.Util;
using Subnautica_Archon.Util.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Behavior.TransferTypes;
using AVS.Assets;
using UnityEngine;
using AVS.Util;
using Behavior.Util.Math;
using Object = UnityEngine.Object;


namespace Subnautica_Archon.Adapters
{

    public class DockableVehicle : IDockable
    {
        private FieldAdapter<Player.Mode> Mode { get; }

        public LogWriter Log { get; }
        public DockableVehicle(Vehicle vehicle, Archon archon)
        {
            Log = new LogWriter(
                prefix: "Dockable#" + Id,
                "Mod");
            Vehicle = vehicle;
            Abstraction = Vehicle.ToAbstraction();
            Archon = archon;
            IsDrone = Drone.IsOne(Vehicle);
            Log.Write($"IsDrone={IsDrone}, IsPlayerControlledDrone={IsPlayerControlledDrone}");
            //if (!HasPlayer && !IsPlayerControlledDrone)
            //    Log.Warn($"DockableVehicle(): Vehicle {Vehicle.NiceName()} does not have a player mounted. mounted vehicle = {Player.main.currentMountedVehicle.NiceName()}, testing = {Vehicle.NiceName()}, IsDrone = {Drone.IsOne(Vehicle)}");
            //else
            //    Log.Write($"DockableVehicle(): HasPlayer={HasPlayer}, IsPlayerControlledDrone={IsPlayerControlledDrone}");
            Mode = FieldAdapter.OfNonPublic<Player.Mode>(Player.main, "mode");
        }
        //private Logging Log { get; } = new Logging(false,"Dockable",true,true);
        public Vehicle Vehicle { get; }
        public IVehicleAbstraction Abstraction { get; }
        public Archon Archon { get; }
        public bool IsDrone { get; }
        public bool HasPlayer => !IsDrone && Player.main.currentMountedVehicle == Vehicle;
        public bool IsPlayerControlledDrone => Drone.Access(Vehicle, out var d) && d.IsPlayerControlling();

        public GameObject GameObject => Vehicle.gameObject;
        private int UpdateCounter { get; set; }
        private static int idCounter = 0;
        public int Id { get; } = ++idCounter;


        public bool ShouldUnfreezeImmediately => Abstraction.IsVanilla;

        public bool UndockUpright => true;


        public override string ToString()
            => $"Dockable{{{Vehicle.NiceName()}, '{Vehicle.GetVehicleName()}'}}";

        private Bounds? bounds;
        public Bounds LocalBounds
        {
            get
            {
                if (bounds.IsNull())
                    bounds = Vehicle.transform.ComputeScaledLocalBounds(includeRenderers: false, includeColliders: true);
                return bounds.Value;
            }
        }

        private Sprite? image;
        private bool imageLoaded = false;
        public Sprite? Image
        {
            get
            {
                if (!imageLoaded)
                {
                    imageLoaded = true;
                    using var log = new LogContext(Log, nameof(Image));
                    var tt = CraftData.GetTechType(Vehicle.gameObject);
                    if (tt != TechType.None)
                    {
                        log.Write($"Fetching image for {tt.AsString()}" );
                        image = SpriteManager.Get(tt, null);
                        if (image.IsNull() || image.texture.IsNull())
                        {
                            log.Error($"Image for {tt.AsString()} does not exist. Using empty texture.");
                        }
                        else
                            log.Write($"Image for {tt.AsString()} is {image.NiceName()}");
                        
                    }
                    else
                    {
                        log.Error($"Unable to get TechType for {Vehicle.NiceName()}");
                        image = null;
                    }
                }
                return image;
            }
        }

        private Sprite[]? moduleSprites = null;
        public Sprite[] Modules
        {
            get
            {
                if (moduleSprites != null)
                    return moduleSprites;
                using var log = new LogContext(Log, nameof(Modules));
                var list = new List<Sprite>();
                int at = 0;
                foreach (InventoryItem mod in (IItemsContainer)Vehicle.modules)
                {
                    at++;
                    if (mod.techType != TechType.None)
                    {
                        var sprite = SpriteManager.Get(mod.techType, null);

                        if (sprite != null)
                        {
                            log.Write($"Image for {at} {mod.techType.AsString()} is {sprite.NiceName()} @r={sprite.rect}, tr={sprite.textureRect}, tro={sprite.textureRectOffset}");
                            list.Add(sprite);
                        }
                    }
                }
                return moduleSprites = list.ToArray();
            }
        }

        public string Name => Vehicle.GetVehicleName();

        public string ClassName => Vehicle.GetType().Name + " Class";

        private static Text Classify(string template, float current, float max, bool plus)
        {
            var text = string.Format(template, current.Percentage(max));
            if (plus)
            {
                text += " >>";
            }

            if (current < max * 0.25f)
                return Text.Error(text);
            if (current < max * 0.5f)
                return Text.Warning(text);
            return Text.Info(text);
        }
        private static Text ClassifyDepth(string template, float current, float max)
        {
            //if (current <= 0)
            //    return Text.Error($"{name}: {current}/{max}");
            var text = string.Format(template, M.Round(current, 0), M.Round(max, 0));
            if (current > max)
                return Text.Error(text);
            if (current > max * 0.95f)
                return Text.Warning(text);
            return Text.Info(text);
        }

        public Text HealthText
        {
            get
            {
                var mixin = Vehicle.liveMixin;
                if (mixin.IsNull())
                {
                    return Text.Error(Language.main.Get("Dockable.Text.HealthUnknown"));
                }
                return Classify(Language.main.Get("Dockable.Text.Health"), mixin.health, mixin.maxHealth, mixin.health < mixin.maxHealth && Archon.WillRepairDocked);
            }
        }
        public Text PowerText
        {
            get
            {
                var energy = Vehicle.GetComponent<EnergyInterface>();
                if (energy.IsNull())
                {
                    return Text.Error(Language.main.Get("Dockable.Text.PowerUnknown"));
                }
                energy.GetValues(out var charge, out var capacity);
                return Classify(Language.main.Get("Dockable.Text.Power"), charge, capacity, charge < capacity && Archon.WillRechargingDocked);
            }
        }


        public Text CrushText
        {
            get
            {
                var d = Archon.crushDamage.GetDepth();
                var max = Vehicle.crushDamage.crushDepth;
                return ClassifyDepth(Language.main.Get("Dockable.Text.Depth"), d, max);
            }
        }

        public Text StorageText
        {
            get
            {
                int count = 0;
                int total = 0;
                for (int i = 0; i < 20; i++)
                {
                    try
                    {
                        var storage = Vehicle.GetStorageInSlot(i, TechType.VehicleStorageModule);
                        if (storage != null)
                        {
                            count += storage.sizeX * storage.sizeY;
                            total += storage.Sum(x => x.width * x.height);
                        }
                    }
                    catch (IndexOutOfRangeException)
                    { }//odd but w/e
                }
                return Text.Info(Language.main.GetFormat("Dockable.Text.Storage", count, total));
            }
        }

        public void RestoreDockedStateFromSaveGame()
        {
            Vehicle.liveMixin.shielded = true;
            Vehicle.crushDamage.enabled = false;
            Vehicle.docked = true;
            Abstraction.DockVehicle();


            if (Drone.Access(Vehicle, out var d))
                d.isAsleep = true;

            //AddToQuickbar(true);

        }

        private void CheckPingInstanceIsDeactivated()
        {
            Log.Debug($"Checking ping instance for {Vehicle.NiceName()}");
            var pi = Abstraction.PingInstance;
            if (pi.enabled || pi.visible)
            {
                Log.Write($"HudPingInstance on {Vehicle.NiceName()} is still enabled. Disabling it");
                pi.SetHudIcon(false);
            }
        }

        private void SignalVehicleDocked()
        {
            Abstraction.DockVehicle();
            CheckPingInstanceIsDeactivated();
        }

        public void BeginDocking()
        {
            moduleSprites = null;
            if (HasPlayer)
            {
                Helper.ChangeAvatarInput(false);
            }
            else if (Drone.Access(Vehicle, out var d))
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


            if (!Abstraction.IsVanilla
                || !HasPlayer)    //otherwise the hands are all wrong
                Vehicle.docked = true;
            Vehicle.liveMixin.shielded = true;
            Vehicle.crushDamage.enabled = false;

            Abstraction.PingInstance.SetHudIcon(false);
        }


        private IEnumerator SwitchToArchon()
        {
            Log.Write("(Re-)Switching player to archon");

            if (HasPlayer)
            {
                new MethodAdapter(Vehicle, "OnPilotModeEnd").Invoke();
                SignalVehicleDocked();
                Vehicle.DeselectSlots();
            }
            else
                Log.Warn($"Docking vehicle does not have the player");

            yield return new WaitForFixedUpdate();
            yield return new WaitForEndOfFrame();

            Log.Write($"Entering archon from transform parent {Player.main.transform.parent.NiceName()}");
            Archon.EnterFromDocking();
            Log.Write($"Registering fix parent {Player.main.transform.parent.NiceName()}");
            UpdateCounter = 0;
            Log.Write($"Player transform parent now {Player.main.transform.parent.GetPath()}");
            Log.Write($"Player vehicle now {Player.main.GetVehicle()} / {Player.main.GetVehicle().SafeGetTransform().GetPath()}");
            Log.Write($" A-Okay = {AvsUtils.FindVehicleInParents(Player.main.transform, out _, new List<Transform>())}");
            Helper.ChangeAvatarInput(true);
            yield break;

            //Archon.SuspendTetherChecks();
            //Player.main.ToNormalMode(findNewPosition: false);
            //Log.Write("Zeroing velocity");
            //Player.main.rigidBody.angularVelocity = Vector3.zero;
            //Log.Write("Exiting locked mode");
            //Player.main.ExitLockedMode(respawn: false, findNewPosition: false);
            //Player.main.SetPosition(Archon.Com.Helms.First().AnyExitLocation);
            //Log.Write("Exiting sitting mode");
            //Player.main.ExitSittingMode();

            //yield return new WaitForFixedUpdate();
            //yield return new WaitForEndOfFrame();

            //Log.Write($"Cleaning up");
            //{
            //    GameInput.ClearInput();
            //    Player.main.transform.parent = null;
            //    Player.main.transform.localScale = Vector3.one;
            //    Player.main.currentMountedVehicle = null;
            //    Player.main.playerController.SetEnabled(enabled: true);
            //    Mode.Set(Player.Mode.Normal);
            //    //Player.main.mode = Player.Mode.Normal;
            //    Player.main.playerModeChanged?.Trigger(Player.Mode.Normal);
            //    Player.main.sitting = false;
            //    Player.main.playerController.ForceControllerSize();
            //}

            //yield return new WaitForFixedUpdate();
            //yield return new WaitForEndOfFrame();

            //Log.Write($"Entering archon from transform parent {Player.main.transform.parent}");
            //Archon.EnterFromDocking();
            //FixParentTo = Player.main.transform.parent;
            //UpdateCounter = 0;
            //Log.Write($"Player transform parent now {Player.main.transform.parent.GetPath()}");
            //Log.Write($"Player vehicle now {Player.main.GetVehicle()} / {Player.main.GetVehicle().SafeGetTransform().GetPath()}");
            //Log.Write($"A-Okay = {AVS.Admin.Utils.FindVehicleInParents(Player.main.transform, out _, new List<Transform>())}");
            //Helper.ChangeAvatarInput(true);
        }


        public void EndDocking()
        {

            //if (Vehicle is ModVehicle mv)
            {
                //CraftData.
                //var module = ModVehicleUndockModule.GetPrototypeFor( mv );

                Vehicle.docked = true;

                //AddToQuickbar(false);


            }

            if (HasPlayer)
            {

                Vehicle.StartCoroutine(SwitchToArchon());

            }
            else
                Log.Write($"Not switching to archon, no player present");
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
                Log.Write($"Player transform parent now {Player.main.transform.parent.GetPath()}");
                Log.Write($"Player vehicle now {Player.main.GetVehicle().NiceName()} / {Player.main.GetVehicle().SafeGetTransform().GetPath()}");
                Log.Write($"A-Okay = {AvsUtils.FindVehicleInParents(Player.main.transform, out _, new List<Transform>())}");
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
                CheckPingInstanceIsDeactivated();
                if (!AvsUtils.FindVehicleInParents(Player.main.transform, out var v, new List<Transform>()))
                {
                    Log.Error($"Unable to find mounted vehicle in player parent(s) at update #{UpdateCounter}. Did find {v.NiceName()}");
                    //if (FixParentTo)
                    //{
                    //    Vehicle.StartCoroutine(SwitchToArchon());
                    //    //Player.main.transform.parent = FixParentTo;

                    //    if (AVS.Admin.Utils.FindVehicleInParents(Player.main.transform, out _, new List<Transform>()))
                    //    {
                    //        Log.Write($"Fixed to {FixParentTo.GetPath()}");
                    //    }
                    //    else
                    //    {
                    //        Log.Error($"Fix failed (tried {FixParentTo.GetPath()})");
                    //        FixParentTo = null;
                    //    }
                    //}
                    //else
                    //    Log.Error($"Cannot fix. No correction target memorized");
                }
            }

        }

        private void SwitchToUndockingCraft()
        {

            Archon.SuspendAutoLeveling();
            try
            {
                if (Archon.IsPlayerControlling())
                    Archon.DeselectSlots();
                if (Archon.IsPlayerInside())
                    Archon.ClosestPlayerExit(false);

                Abstraction.BeginHelmControl();


                Helper.ChangeAvatarInput(false);
                Mode.Set(Player.Mode.LockedPiloting);
            }
            finally
            {
                Archon.RestoreAutoLeveling();
            }
        }


        public void PrepareUndocking()
        {
            if (Drone.IsOne(Vehicle))
            {
            }
            else
            {
                if (Abstraction.IsVanilla)
                {
                    Vehicle.docked = false;//early unset for vanilla or hands are all wrong
                }
                SwitchToUndockingCraft();
                if (Vehicle is Exosuit e)
                {
                    FieldAdapter.OfNonPublic<bool>(e, "onGround").Set(false);
                }
                //else
                //    ChangeAvatarInput(false);
            }

            //Log.Write($"Destroying pickupable (if any)");
            //Object.Destroy(Vehicle.GetComponent<Pickupable>());
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
            Abstraction.UndockVehicle(boardPlayer: true);


            if (Drone.Access(Vehicle, out var d))
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

        public void Tag(string tag)
        {
            var name = Vehicle.GetName();
            if (!name.Contains(tag))
            {
                Log.Write($"Tagging {Vehicle.NiceName()} '{name}' with '{tag}'");
                name += tag;
                Vehicle.SetName(name);
            }
        }

        public void Untag(string tag)
        {
            var name = Vehicle.GetName();
            var idx = name.IndexOf(tag);
            if (idx >= 0)
            {
                Log.Write($"Stripping tag from {Vehicle.NiceName()} '{name}' ('{tag}')");
                name = name.Remove(idx, tag.Length);
                Vehicle.SetName(name);
            }
        }

        public bool IsTagged(string tag)
        {
            return Vehicle.GetName().Contains(tag);
        }

        public void OnUndockedForSaving()
        {
            Log.Write(nameof(OnUndockedForSaving));
            try
            {
                Vehicle.liveMixin.shielded = false;
                Vehicle.crushDamage.enabled = true;

                Vehicle.docked = false;

                Abstraction.UndockVehicle(boardPlayer: false);

                if (Drone.Access(Vehicle, out var d))
                    d.isAsleep = false;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public void OnRedockedAfterSaving()
        {
            Log.Write(nameof(OnRedockedAfterSaving));
            try
            {
                Vehicle.liveMixin.shielded = true;
                Vehicle.crushDamage.enabled = false;

                Vehicle.docked = true;

                Abstraction.DockVehicle();


                if (Drone.Access(Vehicle, out var d))
                    d.isAsleep = true;

            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

#if false
        private void AddToQuickbar(bool fromLoading)
        {
            Log.Write($"Trying to set module slot for {Vehicle.NiceName()}");


            var pu = Vehicle.gameObject.GetComponent<Pickupable>();
            if (!pu)
            {
                Log.Write($"Attaching new Pickupable");
                pu = Vehicle.gameObject.AddComponent<Pickupable>();
            }
            else
                Log.Write($"Pickupable existed");
            TechType myTT = pu.GetTechType();
            var tt = CraftData.GetTechType(Vehicle.gameObject);
            Atlas.Sprite thisAtlasSprite = SpriteManager.Get(myTT);

            var sprite = SpriteHelper.CreateSpriteFromAtlasSprite(thisAtlasSprite);

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
                    if (Archon.modules.GetItemInSlot(slot.ID).IsNull())
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
                    if (fromLoading)
                        Archon.SignalQuickslotsChangedWhileLoading(addedTo.Value);
                    else if (!HasPlayer && !IsPlayerControlledDrone)
                    {
                        Archon.SignalQuickslotsChangedWhilePiloting(addedTo.Value);
                    }
                }
                else
                    Log.Error($"Unable to find suitable quickslot for docked sub {pu}. Sub will not be listed in quickbar");

                Log.Write($"Mod added");
            }
        }
#endif

        public void OpenStorage()
        {
            try
            {
                for (int i = 0; i < 20; i++)
                {
                    var storage = Vehicle.GetStorageInSlot(i, TechType.VehicleStorageModule);
                    if (storage != null)
                    {
                        PDA pda = Player.main.GetPDA();
                        Inventory.main.SetUsedStorage(storage);
                        pda.Open(PDATab.Inventory);
                        return;
                    }
                }
                Log.Warn($"No storage found in {Vehicle.NiceName()} to open");
            }
            catch (Exception ex)
            {
                Log.Error($"Error while opening storage", ex);
            }
        }

        private void OnClosePDA(PDA pda)
        {
            Log.Write($"PDA closed after opening modules");
            try
            {
                Archon.Control.SignalDockedChange(this);
            }
            catch (Exception ex)
            {
                Log.Error($"Error while signaling docked change after closing PDA", ex);
            }
        }
        public void OpenModules()
        {
            try
            {

                PDA pDA = Player.main.GetPDA();
                Inventory.main.SetUsedStorage(Vehicle.modules);
                if (!pDA.Open(PDATab.Inventory, onCloseCallback: OnClosePDA))
                {
                    OnClosePDA(pDA);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error while opening modules", ex);
            }
        }
    }
}