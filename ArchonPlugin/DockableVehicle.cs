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

        public Vehicle Vehicle { get; }
        public Archon Archon { get; }
        public bool HasPlayer { get; }

        public GameObject GameObject => Vehicle.gameObject;

        public void BeginDocking()
        {
            if (HasPlayer)
                AvatarInputHandler.main.gameObject.SetActive(value: false);
            Vehicle.liveMixin.shielded = true;
            Vehicle.crushDamage.enabled = false;
        }


        public void EndDocking()
        {
            if (HasPlayer)
            {
                Player.main.ToNormalMode(findNewPosition: false);
                Player.main.rigidBody.angularVelocity = Vector3.zero;
                Player.main.ExitLockedMode(respawn: false, findNewPosition: false);
                //Player.main.SetPosition(dockedVehicleExitPosition.position);
                Player.main.ExitSittingMode();
                Archon.PlayerEntry();
                Archon.BeginPiloting();
                AvatarInputHandler.main.gameObject.SetActive(value: true);
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