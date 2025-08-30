using Assets.Behavior.TransferTypes;
using AVS.Log;
using AVS.Util;
using Behavior.Util.Math;
using Subnautica_Archon.Adapters.VehicleAbstraction;
using Subnautica_Archon.Util;
using Subnautica_Archon.Util.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Subnautica_Archon.Adapters
{

    public class DockableVehicle : IDockable
    {
        private FieldAdapter<Player.Mode> Mode { get; }

        public DockableVehicle(Vehicle vehicle, Archon archon)
        {
            using var log = SmartLog.For(archon.Owner, tags: "Dockable#" + Id);
            Vehicle = vehicle;
            Abstraction = Vehicle.ToAbstraction(archon.Owner);
            Archon = archon;
            IsDrone = Drone.IsOne(Vehicle);
            log.Write($"IsDrone={IsDrone}, IsPlayerControlledDrone={IsPlayerControlledDrone}");
            Mode = FieldAdapter.OfNonPublic<Player.Mode>(Player.main, "mode");
        }
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

        private Bounds3? bounds;
        public Bounds3 LocalBounds
        {
            get
            {
                if (bounds.IsNull())
                {
                    bounds = Vehicle.transform.ComputeScaledLocalBounds(includeRenderers: false, includeColliders: true,
                        Player.main.SafeGetTransform());
                    //
                    //
                    // var debugObject = new GameObject("DebugBounds");
                    // debugObject.transform.SetParent(Vehicle.transform);
                    // debugObject.transform.localPosition = Vector3.zero;
                    // debugObject.transform.localRotation = Quaternion.identity;
                    // debugObject.transform.localScale = M.V3(1f / Vehicle.transform.localScale.x, 1f / Vehicle.transform.localScale.y, 1f / Vehicle.transform.localScale.z);;
                    //
                    // var box1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    // Object.Destroy(box1.GetComponent<BoxCollider>());
                    // box1.transform.SetParent(debugObject.transform);
                    // box1.transform.localPosition = bounds.Value.Center + M.V3(bounds.Value.Size.x + 1,0,0);
                    // box1.transform.localRotation = Quaternion.identity;
                    // box1.transform.localScale = M.V3(0.1f,bounds.Value.Size.y,bounds.Value.Size.z);
                    //
                    // var box2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    // Object.Destroy(box2.GetComponent<BoxCollider>());
                    // box2.transform.SetParent(debugObject.transform);
                    // box2.transform.localPosition = bounds.Value.Center + M.V3(0,bounds.Value.Size.y + 1,0);
                    // box2.transform.localRotation = Quaternion.identity;
                    // box2.transform.localScale = M.V3(bounds.Value.Size.x,0.1f,bounds.Value.Size.z);
                    //
                    // var box3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    // Object.Destroy(box3.GetComponent<BoxCollider>());
                    // box3.transform.SetParent(debugObject.transform);
                    // box3.transform.localPosition = bounds.Value.Center + M.V3(0,0,bounds.Value.Size.z + 1);
                    // box3.transform.localRotation = Quaternion.identity;
                    // box3.transform.localScale = M.V3(bounds.Value.Size.x,bounds.Value.Size.y,0.1f);
                    //

                }

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
                    using var log = SmartLog.For(Archon.Owner, tags: "Dockable#" + Id);
                    var tt = CraftData.GetTechType(Vehicle.gameObject);
                    if (tt != TechType.None)
                    {
                        log.Write($"Fetching image for {tt.AsString()}");
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
                using var log = SmartLog.For(Archon.Owner, tags: "Dockable#" + Id);
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

        private int numStorageModules = -1;

        public int StorageCount
        {
            get
            {
                if (numStorageModules >= 0)
                    return numStorageModules;
                int count = IterateStorages().Count();

                numStorageModules = count;
                return numStorageModules;
            }
        }

        private void ClearCachedData()
        {
            moduleSprites = null;
            numStorageModules = -1;
            storageText = null;
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

        private Text? storageText = null;

        public Text StorageText
        {
            get
            {
                if (storageText != null)
                    return storageText.Value;
                int count = 0;
                int total = 0;
                foreach (var s in IterateStorages())
                {
                    total += s.sizeX * s.sizeY;
                    count += s.Sum(x => x.width * x.height);
                }
                storageText = Text.Info(Language.main.GetFormat("Dockable.Text.Storage", count, total));
                return storageText.Value;
            }
        }

        private IEnumerable<ItemsContainer> IterateStorages()
        {
            if (Vehicle is Exosuit ex)
            {
                yield return ex.storageContainer.container;
                yield break;
            }

            var innateStorage = Vehicle.GetType().Assembly.GetType("InnateStorageContainer", false);
            if (innateStorage != null)
            {
                var storage2 = Vehicle.GetComponentsInChildren(innateStorage, includeInactive: true);
                foreach (var s in storage2)
                {
                    var a0 = PropertyAdapter.OfPublic<ItemsContainer>(Archon.Owner, s, "container");
                    if (a0.IsValid)
                    {
                        yield return a0.Value;
                    }
                    else
                    {
                        var a1 = PropertyAdapter.OfPublic<ItemsContainer>(Archon.Owner, s, "Container");
                        if (a1.IsValid)
                        {
                            yield return a1.Value;
                        }
                    }
                }
            }
            List<ItemsContainer> storages = new List<ItemsContainer>();
            for (int i = 0; i < 20; i++)
            {
                try
                {
                    var storage = Vehicle.GetStorageInSlot(i, TechType.VehicleStorageModule);
                    if (storage != null)
                    {
                        storages.Add(storage);
                    }
                }
                catch (IndexOutOfRangeException)
                { }//odd but w/e
            }
            foreach (var s in storages)
                yield return s;
        }


        public void RestoreDockedStateFromSaveGame()
        {
            using var log = SmartLog.For(Archon.Owner, tags: "Dockable#" + Id);

            Vehicle.liveMixin.shielded = true;
            Vehicle.crushDamage.enabled = false;
            Vehicle.docked = true;
            Abstraction.DockVehicle();


            if (Drone.Access(Vehicle, out var d))
            {
                log.Write($"Redocking craft is drone. Setting isAsleep to true");
                d.isAsleep = true;
            }

            //AddToQuickbar(true);

        }

        private void CheckPingInstanceIsDeactivated()
        {
            using var log = SmartLog.For(Archon.Owner, tags: "Dockable#" + Id);
            log.Write($"Checking ping instance for {Vehicle.NiceName()}");
            var pi = Abstraction.PingInstance;
            if (pi.enabled || pi.visible)
            {
                log.Write($"HudPingInstance on {Vehicle.NiceName()} is still enabled. Disabling it");
                pi.SetHudIcon(log, false);
            }
        }

        private void SignalVehicleDocked()
        {
            using var log = SmartLog.For(Archon.Owner, tags: "Dockable#" + Id);
            Abstraction.DockVehicle();
            CheckPingInstanceIsDeactivated();
        }

        public void BeginDocking()
        {
            using var log = SmartLog.For(Archon.Owner, tags: "Dockable#" + Id);
            ClearCachedData();
            if (HasPlayer)
            {
                Helper.ChangeAvatarInput(log, false);
            }
            else if (Drone.Access(Vehicle, out var d))
            {
                if (IsPlayerControlledDrone)
                {

                    log.Write($"Stopping drone control");
                    d.StopControlling();

                    if (IsPlayerControlledDrone)
                        log.Warn($"StopControlling() has not been successful.");
                    else
                    {
                        log.Write($"Player has stopped controlling drone");
                    }

                    Helper.ChangeAvatarInput(log, true);
                    if (!Player.main.ToNormalMode(false) && Mode != Player.Mode.Normal)
                    {
                        log.Write($"ToNormalMode() refused and mode is not normal. Forcing to normal");
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

            Abstraction.PingInstance.SetHudIcon(log, false);
        }


        private IEnumerator SwitchToArchon(SmartLog log)
        {
            log.Write("(Re-)Switching player to archon");

            if (HasPlayer)
            {
                new MethodAdapter(Archon.Owner, Vehicle, "OnPilotModeEnd").Invoke();
                SignalVehicleDocked();
                Vehicle.DeselectSlots();
            }
            else
                log.Warn($"Docking vehicle does not have the player");

            yield return new WaitForFixedUpdate();
            yield return new WaitForEndOfFrame();

            //using var log2 = new LogContext(log, nameof(SwitchToArchon));

            log.Write($"Entering archon from transform parent {Player.main.transform.parent.NiceName()}");
            Archon.EnterFromDocking();
            log.Write($"Registering fix parent {Player.main.transform.parent.NiceName()}");
            UpdateCounter = 0;
            log.Write($"Player transform parent now {Player.main.transform.parent.GetPath()}");
            log.Write($"Player vehicle now {Player.main.GetVehicle()} / {Player.main.GetVehicle().SafeGetTransform().GetPath()}");
            log.Write($" A-Okay = {AvsUtils.FindVehicleInParents(Player.main.transform, out _, [])}");
            Helper.ChangeAvatarInput(log, true);
        }


        public void EndDocking()
        {
            using var log = SmartLog.For(Archon.Owner, tags: "Dockable#" + Id);

            Vehicle.docked = true;

            if (HasPlayer)
            {
                Archon.Owner.StartModCoroutine(
                    nameof(DockableVehicle) + '.' + nameof(SwitchToArchon),
                    SwitchToArchon);
            }
            else
                log.Write($"Not switching to archon, no player present");
        }


        public void OnDockingDone()
        {
            using var log = SmartLog.For(Archon.Owner, tags: "Dockable#" + Id);

            if (HasPlayer)
            {
                log.Write($"Player transform parent now {Player.main.transform.parent.GetPath()}");
                log.Write($"Player vehicle now {Player.main.GetVehicle().NiceName()} / {Player.main.GetVehicle().SafeGetTransform().GetPath()}");
                log.Write($"A-Okay = {AvsUtils.FindVehicleInParents(Player.main.transform, out _, new List<Transform>())}");
            }
        }

        public void UpdateWaitingForBayDoorClose()
        {
            UpdateCounter++;
            if (HasPlayer)
            {
                using var log = SmartLog.For(Archon.Owner, tags: "Dockable#" + Id);
                CheckPingInstanceIsDeactivated();
                if (!AvsUtils.FindVehicleInParents(Player.main.transform, out var v, new List<Transform>()))
                {
                    Log.Error($"Unable to find mounted vehicle in player parent(s) at update #{UpdateCounter}. Did find {v.NiceName()}");
                }
            }

        }

        private void SwitchToUndockingCraft()
        {
            using var log = SmartLog.For(Archon.Owner, tags: "Dockable#" + Id);

            Archon.SuspendAutoLeveling();
            try
            {
                if (Archon.IsPlayerControlling())
                    Archon.DeselectSlots();
                if (Archon.IsPlayerInside())
                    Archon.ClosestPlayerExit(false);

                Abstraction.BeginHelmControl();


                Helper.ChangeAvatarInput(log, false);
                Mode.Set(Player.Mode.LockedPiloting);
            }
            finally
            {
                Archon.RestoreAutoLeveling();
            }
        }


        public void PrepareUndocking()
        {
            using var log = SmartLog.For(Archon.Owner, tags: "Dockable#" + Id);

            if (Drone.IsOne(Vehicle))
            {
                log.Write($"Undocking craft is drone. No action necessary");
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
            }

        }



        public void UpdateWaitingForBayDoorOpen()
        {
        }

        public void BeginUndocking()
        {
            using var log = SmartLog.For(Archon.Owner, tags: "Dockable#" + Id);
            Abstraction.PingInstance.SetHudIcon(log, true);
        }

        public void EndUndocking()
        {
            using var log = SmartLog.For(Archon.Owner, tags: "Dockable#" + Id);
            Vehicle.liveMixin.shielded = false;
            Vehicle.crushDamage.enabled = true;
            Vehicle.docked = false;


            Abstraction.UndockVehicle(boardPlayer: !IsDrone);
            Abstraction.PingInstance.SetHudIcon(log, true);

            if (!Vehicle.subName.pingInstance.isActiveAndEnabled ||
                !Vehicle.subName.pingInstance.gameObject.activeInHierarchy || !Vehicle.subName.pingInstance.visible)
                log.Warn($"There appears to be an issue with the ping instance: {Vehicle.subName.pingInstance.isActiveAndEnabled}, {Vehicle.subName.pingInstance.gameObject.activeInHierarchy}, {Vehicle.subName.pingInstance.visible}");
            var ef = Vehicle.GetComponent<EnergyInterface>();
            if (ef.IsNotNull() && !ef.enabled || !ef.gameObject.activeInHierarchy)
                log.Warn($"There appears to be an issue with the energy interface: {ef.enabled}, {ef.gameObject.activeInHierarchy}");


            if (Drone.Access(Vehicle, out var d))
            {
                log.Write($"Undocking craft is drone. Setting isAsleep to false");
                d.isAsleep = false;
            }
            else
                Helper.ChangeAvatarInput(log, true);
        }

        public void OnUndockingDone()
        {
        }


        public IEnumerable<T> GetAllComponents<T>() where T : Component
        {
            return Vehicle.GetComponentsInChildren<T>()
                .Where(x => !x.transform.IsChildOf(Player.mainObject.transform));
        }

        public void OnUndockedForSaving()
        {
            using var log = SmartLog.For(Archon.Owner, tags: "Dockable#" + Id);
            try
            {
                Vehicle.liveMixin.shielded = false;
                Vehicle.crushDamage.enabled = true;

                Vehicle.docked = false;

                Abstraction.UndockVehicle(boardPlayer: false);

                if (Drone.Access(Vehicle, out var d))
                {
                    log.Write($"Undocking craft is drone. Setting isAsleep to false");
                    d.isAsleep = false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public void OnRedockedAfterSaving()
        {
            using var log = SmartLog.For(Archon.Owner, tags: "Dockable#" + Id);
            try
            {
                Vehicle.liveMixin.shielded = true;
                Vehicle.crushDamage.enabled = false;

                Vehicle.docked = true;

                Abstraction.DockVehicle();


                if (Drone.Access(Vehicle, out var d))
                {
                    log.Write($"Redocking craft is drone. Setting isAsleep to false");
                    d.isAsleep = true;
                }

            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

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
            using var log = SmartLog.For(Archon.Owner, tags: "Dockable#" + Id);

            log.Write($"PDA closed after opening modules");
            try
            {
                ClearCachedData();
                Archon.Control.SignalDockedChange(this);
            }
            catch (Exception ex)
            {
                log.Error($"Error while signaling docked change after closing PDA", ex);
            }
        }
        public void OpenModules()
        {
            using var log = SmartLog.For(Archon.Owner, tags: "Dockable#" + Id);
            try
            {

                PDA pda = Player.main.GetPDA();
                Inventory.main.SetUsedStorage(Vehicle.modules);
                if (!pda.Open(PDATab.Inventory, onCloseCallback: OnClosePDA))
                {
                    OnClosePDA(pda);
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error while opening modules", ex);
            }
        }

        public void OpenStorage(int storageIndex)
        {
            using var log = SmartLog.For(Archon.Owner, tags: "Dockable#" + Id);
            var s = IterateStorages().ElementAtOrDefault(storageIndex);
            if (s != null)
            {
                try
                {
                    PDA pda = Player.main.GetPDA();
                    Inventory.main.SetUsedStorage(s);
                    if (!pda.Open(PDATab.Inventory, onCloseCallback: OnClosePDA))
                    {
                        OnClosePDA(pda);
                    }
                }
                catch (Exception ex)
                {
                    log.Error($"Error while opening storage #{storageIndex}", ex);
                }
            }
            else
            {
                log.Warn($"No storage found in {Vehicle.NiceName()} at index {storageIndex} to open");
            }
        }
    }
}