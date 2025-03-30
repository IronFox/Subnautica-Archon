
using System.Collections.Generic;
using UnityEngine;
using UWE;
using VehicleFramework.Admin;
using VehicleFramework;
using System.Reflection;
using System.Linq;
using System;
using System.Collections;

namespace Subnautica_Archon
{
    public class ArchonDockingBay : MonoBehaviour
    {
        private Transform vehicleDockedPosition = null;

        private Transform dockedVehicleExitPosition = null;

        private Transform vehicleDockingTrigger = null;

        private Coroutine dockAnimation = null;

        private float dockingDistanceThreshold = 6f;

        private Vector3 internalExitForce;

        private bool isInitialized = false;

        public Vehicle currentDockedVehicle { get; protected set; }

        public bool Initialize(Transform docked, Transform exit, Transform dockTrigger, Vector3 exitForce)
        {
            string text = "Dockingbay Initialization Error: Input transform was null: Input transform was null:";
            if (docked == null)
            {
                Log.Error(text + " docked");
                return false;
            }

            if (exit == null)
            {
                Log.Write(text + " exit");
                return false;
            }

            if (dockTrigger == null)
            {
                Log.Write(text + " dockTrigger");
                return false;
            }

            vehicleDockedPosition = docked;
            dockedVehicleExitPosition = exit;
            vehicleDockingTrigger = dockTrigger;
            internalExitForce = exitForce;
            isInitialized = true;
            return true;
        }

        public void Detach(bool withPlayer)
        {
            if (dockAnimation == null && IsSufficientSpace())
            {
                CoroutineHost.StartCoroutine(InternalDetach(withPlayer));
            }
        }

        protected virtual bool IsSufficientSpace()
        {
            return true;
        }

        protected virtual void TryRechargeDockedVehicle()
        {
        }

        protected virtual Vehicle GetDockingTarget()
        {
            Vehicle result = null;
            float num = 99999f;
            Vehicle vehicle = GameObjectManager<Vehicle>.FindNearestSuch(base.transform.position);
            if (vehicle)
            {
                float num2 = Vector3.Distance(vehicleDockingTrigger.transform.position, vehicle.transform.position);
                if (num2 < num)
                {
                    result = vehicle;
                    num = num2;
                }
            }
            if (!result)
                result = GameObjectManager<Vehicle>.FindNearestSuch(base.transform.position);
            return result;
        }

        protected virtual void UpdateDockedVehicle()
        {
            currentDockedVehicle.transform.position = vehicleDockedPosition.position;
            currentDockedVehicle.transform.rotation = vehicleDockedPosition.rotation;
            currentDockedVehicle.liveMixin.shielded = true;
            currentDockedVehicle.useRigidbody.detectCollisions = false;
            currentDockedVehicle.crushDamage.enabled = false;

            new MethodAdapter<bool>(currentDockedVehicle, "UpdateCollidersForDocking").Invoke(true);

            if (currentDockedVehicle is SeaMoth)
            {
                (currentDockedVehicle as SeaMoth).toggleLights.SetLightsActive(isActive: false);
                currentDockedVehicle.GetComponent<SeaMoth>().enabled = true;
            }
            else if (currentDockedVehicle is ModVehicle)
            {
                HeadLightsController headlights = (currentDockedVehicle as ModVehicle).headlights;
                if (headlights.IsLightsOn)
                {
                    (currentDockedVehicle as ModVehicle).headlights.Toggle();
                }
            }
        }

        protected virtual void HandleDockDoors(TechType dockedVehicle, bool open)
        {
        }

        protected virtual bool ValidateAttachment(Vehicle dockTarget)
        {
            if (Vector3.Distance(vehicleDockingTrigger.position, dockTarget.transform.position) >= dockingDistanceThreshold)
            {
                return false;
            }

            return true;
        }

        protected virtual void OnDockUpdate()
        {
        }

        protected virtual void OnStartedDocking()
        {
        }

        protected virtual void OnFinishedDocking(Vehicle dockingVehicle)
        {
            bool flag = dockingVehicle is SeaMoth && Player.main.inSeamoth;
            bool flag2 = dockingVehicle is Exosuit && Player.main.inExosuit;
            if (flag || flag2)
            {
                Player.main.rigidBody.velocity = Vector3.zero;
                Player.main.ToNormalMode(findNewPosition: false);
                Player.main.rigidBody.angularVelocity = Vector3.zero;
                Player.main.ExitLockedMode(respawn: false, findNewPosition: false);
                Player.main.SetPosition(dockedVehicleExitPosition.position);
                Player.main.ExitSittingMode();
                Player.main.SetPosition(dockedVehicleExitPosition.position);
                ModVehicle.TeleportPlayer(dockedVehicleExitPosition.position);
            }

            dockingVehicle.transform.SetParent(base.transform);
        }

        protected virtual void OnStartedUndocking(bool withPlayer)
        {
            HandleDockDoors(currentDockedVehicle.GetTechType(), open: true);
            currentDockedVehicle.useRigidbody.velocity = Vector3.zero;
            currentDockedVehicle.transform.SetParent(base.transform.parent);
            if (withPlayer)
            {
                new MethodAdapter<Player, bool, bool>(currentDockedVehicle, "EnterVehicle").Invoke(Player.main, true, true);
                //currentDockedVehicle.EnterVehicle(Player.main, teleport: true, playEnterAnimation: true);
                AvatarInputHandler.main.gameObject.SetActive(value: false);
            }
        }

        protected virtual IEnumerator DoUndockingAnimations()
        {
            currentDockedVehicle.useRigidbody.AddRelativeForce(internalExitForce, ForceMode.VelocityChange);
            while (Vector3.Distance(currentDockedVehicle.transform.position, vehicleDockingTrigger.position) <= dockingDistanceThreshold)
            {
                yield return null;
            }
        }

        protected virtual void OnFinishedUndocking(bool hasPlayer)
        {
            currentDockedVehicle.liveMixin.shielded = false;
            currentDockedVehicle.useRigidbody.detectCollisions = true;
            currentDockedVehicle.crushDamage.enabled = true;
            new MethodAdapter<bool>(currentDockedVehicle, "UpdateCollidersForDocking").Invoke(false);
            if (hasPlayer)
            {
                AvatarInputHandler.main.gameObject.SetActive(value: true);
            }
        }

        protected virtual IEnumerator DoDockingAnimations(Vehicle dockingVehicle, float duration, float duration2)
        {
            yield return CoroutineHost.StartCoroutine(MoveAndRotate(dockingVehicle, vehicleDockingTrigger, duration));
            yield return CoroutineHost.StartCoroutine(MoveAndRotate(dockingVehicle, vehicleDockedPosition, duration2));
        }

        private void Update()
        {
            if (!isInitialized)
            {
                return;
            }

            OnDockUpdate();
            if (dockAnimation == null)
            {
                if (currentDockedVehicle == null)
                {
                    TryAttachVehicle();
                    return;
                }

                HandleDockDoors(currentDockedVehicle.GetTechType(), open: false);
                TryRechargeDockedVehicle();
                UpdateDockedVehicle();
            }
        }

        private void TryAttachVehicle()
        {
            Vehicle dockingTarget = GetDockingTarget();
            if (dockingTarget == null)
            {
                HandleDockDoors(TechType.None, open: false);
                return;
            }

            if (Vector3.Distance(vehicleDockedPosition.position, dockingTarget.transform.position) < 20f)
            {
                HandleDockDoors(dockingTarget.GetTechType(), open: true);
            }
            else
            {
                HandleDockDoors(dockingTarget.GetTechType(), open: false);
            }

            if (ValidateAttachment(dockingTarget))
            {
                CoroutineHost.StartCoroutine(InternalAttach(dockingTarget));
            }
        }

        private IEnumerator InternalAttach(Vehicle dockTarget)
        {
            if (!(dockTarget == null))
            {
                OnStartedDocking();
                dockAnimation = CoroutineHost.StartCoroutine(DoDockingAnimations(dockTarget, 1f, 1f));
                yield return dockAnimation;
                OnFinishedDocking(dockTarget);
                currentDockedVehicle = dockTarget;
                dockAnimation = null;
            }
        }

        private IEnumerator InternalDetach(bool withPlayer)
        {
            if (!(currentDockedVehicle == null))
            {
                OnStartedUndocking(withPlayer);
                dockAnimation = CoroutineHost.StartCoroutine(DoUndockingAnimations());
                yield return dockAnimation;
                OnFinishedUndocking(withPlayer);
                currentDockedVehicle = null;
                dockAnimation = null;
            }
        }

        private IEnumerator MoveAndRotate(Vehicle objectToMove, Transform firstTarget, float duration)
        {
            Vector3 startPosition = objectToMove.transform.position;
            Quaternion startRotation = objectToMove.transform.rotation;
            Vector3 midPosition = firstTarget.position;
            Quaternion midRotation = firstTarget.rotation;
            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                objectToMove.transform.position = Vector3.Lerp(startPosition, midPosition, elapsedTime / duration);
                objectToMove.transform.rotation = Quaternion.Slerp(startRotation, midRotation, elapsedTime / duration);
                yield return null;
            }

            objectToMove.transform.position = midPosition;
            objectToMove.transform.rotation = midRotation;
        }
    }
}