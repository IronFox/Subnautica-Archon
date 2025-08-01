using Assets.Behavior.Adapters;
using Assets.Behavior.Interfaces;
using Assets.Behavior.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ArchonControl : MonoBehaviour
{
    public KeyCode openConsoleKey = KeyCode.F7;

    public Transform interior;
    public Transform interiorColliders;
    public Transform interiorLights;
    public GameObject[] glass;
    public Transform exterior;
    public Transform dockingTrigger;
    public Transform dockedSpace;
    public Transform hangarRoot;
    public Transform exteriorModel;
    public Transform helmSeatRoot;
    //public Renderer[] onEnterDisableShadows;
    public Renderer exteriorInteriorShadowCaster;
    public Renderer interiorExteriorShadowCaster;
    public Material mapHologramMaterial;

    public float forwardAxis;
    public float rightAxis;
    public float upAxis;
    public float zoomAxis;
    public float lookRightAxis;
    public float lookUpAxis;
    public float interiorLightScale = 1;

    private bool isMovingInReverse;

    public const int OuterShellLayer = 30;

    //public bool overdriveActive;
    public bool outOfWater;
    public bool freeCamera = true;
    public bool flipFreeHorizontalRotationInReverse = true;
    public bool flipFreeVerticalRotationInReverse = false;

    public bool doAutoLevel;

    public bool positionCameraBelowSub;
    public float environmentalLeanIntensity = 1;

    public bool zoomedInIsCockpit = true;
    public bool forceCockpitCamera;
    public bool powerOff;
    public bool batteryDead;
    public bool reactorIsCharging;
    public bool openUpgradeCover;
    public bool lights;

    public int maxDockedVehicles = 2;

    public string noDockableTitle = "Nothing docked";

    private DateTime lastOnboarded;
    private IDockable selectedDockable;

    private bool boardedLeave;
    private PlayerReference boardedBy,
                    controlledBy;
    private readonly Undoable controlUndo = new Undoable();
    private readonly FloatTimeFrame energyHistory = new FloatTimeFrame(TimeSpan.FromSeconds(2));
    private ComponentSet<Collider> EnabledColliders { get; } = new ComponentSet<Collider>();
    private ComponentSet<Collider> DisabledColliders { get; } = new ComponentSet<Collider>();
    private bool cameraIsInVehicle;
    public bool CameraIsInVehicle => cameraIsInVehicle;

    public float maxEnergy = 1;
    public float currentEnergy = 0.5f;
    public float maxHealth = 1;
    public float currentHealth = 0.5f;
    public bool isHealing;

    private float forceAutoLevelInSeconds = float.MaxValue;

    private FirstPersonMarkers firstPersonMarkers;

    public float rotationDegreesPerSecond = 20;

    private bool interiorLightsEnabled;
    private EnergyLevel energyLevel;

    private Transform cameraRoot;
    private bool helmCameraIsInVehicle;

    public DriveControl forwardFacingLeft;
    public DriveControl backFacingLeft;
    public DriveControl forwardFacingRight;
    public DriveControl backFacingRight;

    public HealingLight[] healingLights;

    public Transform trailSpace;
    public Transform trailSpaceCameraContainer;
    public StatusConsole statusConsole;

    private RotateCamera rotateCamera;
    private PositionCamera positionCamera;
    private NonCameraOrientation nonCameraOrientation;
    private FallOrientation fallOrientation;
    public BayControl bayControl;
    private HullLightController hullLightController;

    private DirectionalDrag drag;
    private DirectAt orientation;
    private RudderControl[] rudders;
    private Rigidbody rb;
    private EvacuateIntruders evacuateIntruders;
    private bool currentlyControlled;

    private Parentage onboardLocalizedTransform;
    private Parentage cameraMove, seatOrigin;

    private bool currentCameraCenterIsCockpit;
    private bool cameraIsInTrailspace;

    private bool wasEverBoarded;

    private bool shouldBeKinematic;
    public bool IsBeingControlled => currentlyControlled && !forceCockpitCamera;

    private bool ApplyFreeCamera => freeCamera;// || (zoomedInIsCockpit && positionCamera.isFirstPerson));

    /// <summary>
    /// True if the archon assumes the camera is in the cockpit, and the player should be able to turn their head.
    /// </summary>
    public bool ShouldBeAbleToTurnHead => forceCockpitCamera;// || (zoomedInIsCockpit && positionCamera.isFirstPerson);

    private enum CameraState
    {
        IsFree,
        IsBound,
        IsTransitioningToBound
    }

    private CameraState state = CameraState.IsBound;


    public bool IsBoardedButNotControlled => boardedBy.IsSet;

    private float PitchDelta => transform.rotation.eulerAngles.x >= 180 ? 360 - transform.rotation.eulerAngles.x : transform.rotation.eulerAngles.x;
    private float RollDelta => transform.rotation.eulerAngles.z >= 180 ? 360 - transform.rotation.eulerAngles.z : transform.rotation.eulerAngles.z;
    public bool IsLevel => RollDelta < 0.8f && PitchDelta < 0.8f;
    private float checkFloatingCharacterForSeconds;

    private void ChangeState(CameraState state)
    {
        //Debug.Log($"->{state}");
        this.state = state;
    }

    private void MoveCameraToTrailSpace()
    {
        if (!cameraIsInTrailspace)
        {
            cameraIsInTrailspace = true;

            Log.Write("Moving camera to trailspace. Setting secondary fallback camera transform");
            SetCameraIsInVehicle(positionCamera.IsInVehicle, false);

            CameraUtil.secondaryFallbackCameraTransform = trailSpaceCameraContainer;

            cameraMove = Parentage.FromLocal(cameraRoot);
            cameraRoot.parent = trailSpaceCameraContainer;
            Location.LocalIdentity.ApplyTo(cameraRoot);



            if (helmCameraIsInVehicle)
            {
                helmCameraIsInVehicle = false;
                ChangeCameraIsInVehicle(true);
            }


            Log.Write("Moved");
        }
    }

    //internal void RestoreCockpitView(Vector3 lookAt)
    //{
    //    MoveCameraOutOfTrailSpace(false);
    //    if (controlledBy)
    //        controlledBy.LookInDirection(lookAt);
    //}

    private Shader glassShader;


    private void SetCollidersEnabled(IEnumerable<Collider> colliders, bool enable)
    {
        foreach (var c in colliders)
        {
            if (c)
            {
                if (enable)
                {
                    EnabledColliders.Add(c);
                    DisabledColliders.Remove(c);
                    c.enabled = true;
                }
                else
                {
                    DisabledColliders.Add(c);
                    EnabledColliders.Remove(c);
                    c.enabled = false;
                }
            }
        }
    }

    private void UpdateInteriorCollidersAndLights(bool enable)
    {
        Log.Write($"Updating interior colliders and lights: {enable}");
        SetCollidersEnabled(interiorColliders.GetAllColliders(PlayerAdapter.Player()), enable);

        interiorLights.gameObject.SetActive(enable);
        glass?.ForEach(g => g.SetActive(enable));
        interiorLightsEnabled = enable;
    }

    private void MoveCameraOutOfTrailSpace(bool withCollidersAndLights)
    {
        if (cameraIsInTrailspace)
        {
            cameraIsInTrailspace = false;

            Log.Write("Moving camera out of trailspace. Unsetting secondary fallback camera transform");
            SetCameraIsInVehicle(true, withCollidersAndLights);

            CameraUtil.secondaryFallbackCameraTransform = null;

            cameraMove.Restore();


            if (helmCameraIsInVehicle)
            {
                Log.Write($"Restoring helm seat parentage");
                seatOrigin.Restore();
            }


            Log.Write("Moved");
        }
        else
        {
            UpdateInteriorCollidersAndLights(withCollidersAndLights);

        }
    }


    private void SetCollisionActive(Transform t, bool active)
    {
        if (t)
        {
            SetCollidersEnabled(t.GetAllColliders(PlayerAdapter.Player()), active);
            //foreach (var r in t.GetComponentsInChildren<Collider>())
            //    r.enabled = active;
        }
    }
    private void SetRenderAndCollisionActive(Transform t, bool active)
    {
        if (t)
        {
            foreach (var r in t.GetComponentsInChildren<Renderer>())
                r.enabled = active;
            SetCollisionActive(t, active);
        }

    }

    public void Undock(GameObject dockedSub)
    {
        bayControl.Undock(dockedSub);
    }

    public UndockingCheckResult CheckUndocking(GameObject dockedSub)
    {
        return bayControl.CheckUndocking(dockedSub);
    }


    public void SignalLoading()
    {
        bayControl.SignalLoading();
    }


    public void RedetectDocked()
    {
        bayControl.RedetectDocked();
    }

    public void PrepareForSaving()
    {
        Log.Write(nameof(PrepareForSaving));
        bayControl.PrepareForSaving();
    }

    public void Enter(PlayerReference player, bool skipOrientation = false)
    {
        Log.Write($"Boarding");
        RigidbodyUtil.SetKinematic(rb);
        shouldBeKinematic = true;
        transform.eulerAngles = M.V3(0, transform.localEulerAngles.y, 0);

        if (!skipOrientation)
        {
            forceAutoLevelInSeconds = 0;
        }
        else
            forceAutoLevelInSeconds = float.MaxValue;

        checkFloatingCharacterForSeconds = 1;
        boardedBy = player;
        boardedLeave = false;
        SetCameraIsInVehicle(true, true);
        //evacuateIntruders.enabled = true;
    }

    internal void ChangeCameraIsInVehicle(bool isInVehicle)
    {
        if (isInVehicle && !helmCameraIsInVehicle)
        {
            seatOrigin = Parentage.FromLocal(helmSeatRoot);
            helmSeatRoot.SetParent(trailSpaceCameraContainer, false);
            helmSeatRoot.localPosition = Vector3.zero;
            helmSeatRoot.localRotation = Quaternion.identity;
        }
        else if (!isInVehicle && helmCameraIsInVehicle)
        {
            seatOrigin.Restore();
        }

        SetCameraIsInVehicle(isInVehicle, false);



        helmCameraIsInVehicle = isInVehicle;
        if (helmCameraIsInVehicle)
            ReintegrationTrailSpace();
        else
            OffloadTrailSpace();
    }


    internal Vector3 GetHelmCameraCenter()
    {
        return helmCameraIsInVehicle
            ? transform.TransformPoint(seatOrigin.Transform.Position)
            : helmSeatRoot.position;
    }

    private void SetCameraIsInVehicle(bool isInVehicle, bool withCollidersAndLights)
    {
        Log.Write($"Setting camera is in vehicle: {isInVehicle}, withCollidersAndLights = {withCollidersAndLights}");
        cameraIsInVehicle = isInVehicle;
        interior.gameObject.SetActive(isInVehicle);
        UpdateInteriorCollidersAndLights(isInVehicle && withCollidersAndLights);

        exteriorInteriorShadowCaster.enabled = isInVehicle;
        interiorExteriorShadowCaster.enabled = isInVehicle;
        if (exteriorModel)
            exteriorModel.GetComponentsInChildren<Renderer>().ForEach(c => c.shadowCastingMode = isInVehicle ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On);
        Log.Write($"Setting exterior colliders to {!isInVehicle || !withCollidersAndLights}");
        SetRenderAndCollisionActive(exterior, !isInVehicle || !withCollidersAndLights);
        SetCollisionActive(helmSeatRoot, isInVehicle && withCollidersAndLights);
    }

    public void Exit()
    {
        Log.Write($"Offboarding");

        SetCameraIsInVehicle(false, false);

        evacuateIntruders.enabled = false;
        checkFloatingCharacterForSeconds = 0;
        if (boardedBy)
        {
            boardedBy = default;
            RigidbodyUtil.UnsetKinematic(rb);
            shouldBeKinematic = false;
        }
    }


    public void Control(PlayerReference player)
    {
        wasEverBoarded = true;
        checkFloatingCharacterForSeconds = 0;
        lastOnboarded = DateTime.Now;
        forceAutoLevelInSeconds = float.MaxValue;
        if (!currentlyControlled)
        {
            Log.Write($"Controlling");

            controlledBy = player;
            Exit();

            var listeners = BoardingListeners.Of(this, trailSpace);

            listeners.SignalEnterControlBegin();

            cameraRoot = player.CameraRoot;
            if (!cameraRoot)
                cameraRoot = Camera.main.transform;
            Log.Write($"Setting {cameraRoot} as cameraRoot");
            CameraUtil.primaryFallbackCameraTransform = cameraRoot;
            onboardLocalizedTransform = Parentage.FromLocal(cameraRoot);

            cameraIsInTrailspace = false;//just in case
                                         //if (!zoomedInIsCockpit || !positionCamera.isFirstPerson)
            MoveCameraToTrailSpace();
            //else
            //    SetCameraIsInVehicle(true, false);
            if (!helmCameraIsInVehicle)
                OffloadTrailSpace();

            currentlyControlled = true;

            player.DisableCollidersAndRigidbodies(controlUndo);

            listeners.SignalEnterControlEnd();
        }
    }
    private void OffloadTrailSpace()
    {
        if (trailSpace.parent != transform.parent)
        {
            Log.Write($"Offloading trail space");
            trailSpace.parent = transform.parent;
        }

    }

    private void ReintegrationTrailSpace()
    {
        if (trailSpace.parent != transform)
        {
            Log.Write($"Reintegrating trail space");
            trailSpace.parent = transform;
        }
    }

    private bool TrailSpaceShouldBeOffloaded
        => currentlyControlled && !helmCameraIsInVehicle;
    private bool TrailSpaceIsOffloaded
        => trailSpace.parent == transform.parent;

    public bool ExitControl(PlayerReference player, bool skipOrientation = false)
    {
        if (currentlyControlled && !outOfWater)
        {

            Log.Write($"Exiting control");

            controlUndo.UndoAndClear();
            var listeners = BoardingListeners.Of(this, trailSpace);
            try
            {

                listeners.SignalExitControlBegin();

                MoveCameraOutOfTrailSpace(true);
                Log.Write($"Restoring parentage");
                onboardLocalizedTransform.Restore();
            }
            finally
            {
                currentlyControlled = false;
                ReintegrationTrailSpace();
            }
            controlledBy = default;
            Enter(player, skipOrientation);
            listeners.SignalExitControlEnd();
            return true;
        }
        return false;
    }

    // Start is called before the first frame update
    void Start()
    {
        hullLightController = GetComponentInChildren<HullLightController>();
        evacuateIntruders = GetComponentInChildren<EvacuateIntruders>();
        drag = GetComponentInChildren<DirectionalDrag>();
        nonCameraOrientation = GetComponent<NonCameraOrientation>();
        rb = GetComponent<Rigidbody>();
        orientation = GetComponent<DirectAt>();
        rudders = GetComponentsInChildren<RudderControl>();
        rotateCamera = trailSpace.GetComponent<RotateCamera>();
        positionCamera = trailSpace.GetComponent<PositionCamera>();
        fallOrientation = GetComponent<FallOrientation>();
        energyLevel = GetComponentInChildren<EnergyLevel>();
        firstPersonMarkers = GetComponentInChildren<FirstPersonMarkers>();
        //bayControl = GetComponentInChildren<BayControl>();
        if (orientation)
            orientation.targetOrientation = inWaterDirectionSource = new TransformDirectionSource(trailSpace);
        evacuateIntruders.enabled = IsBoardedButNotControlled;
        SetCameraIsInVehicle(false, false);
    }

    private static string TN(RenderTexture rt)
    {
        if (rt == null)
            return "null";
        return $"{rt.name}, ptr = {rt.GetNativeTexturePtr()}";
    }


    private static string AllMessages(Exception ex)
    {
        string rs = ex.Message;
        if (ex.InnerException != null)
            rs += "<-" + AllMessages(ex.InnerException);
        return rs;
    }

    private void LogComposition(Transform t, Indent indent = default)
    {
        new HierarchyAnalyzer().LogToJson(t, $@"C:\Temp\Logs\snapshot{DateTime.Now:yyyy-MM-dd HH_mm_ss}.json");

    }

    private IDirectionSource inWaterDirectionSource;

    public ArchonControl()
    {
    }

    private bool OnboardingCooldown => DateTime.Now - lastOnboarded < TimeSpan.FromSeconds(1);

    private void ProcessUpgradeCover()
    {
        //if (openUpgradeCover)
        //{
        //    if (upgradeCoverAnimation.IsAtBeginning)
        //    {
        //        var hideOnCoverOpen = GetComponentsInChildren<HideIfModuleCoverClosed>();
        //        foreach (var c in hideOnCoverOpen)
        //            c.SignalCoverOpening();
        //    }
        //    else
        //        coverWasOpen = true;
        //    upgradeCoverAnimation.animateForward = true;
        //}
        //else
        //{
        //    upgradeCoverAnimation.animateForward = false;
        //    if (upgradeCoverAnimation.IsAtBeginning)
        //    {
        //        if (coverWasOpen)
        //        {
        //            coverWasOpen = false;
        //            var hideOnCoverOpen = GetComponentsInChildren<HideIfModuleCoverClosed>();
        //            foreach (var c in hideOnCoverOpen)
        //                c.SignalCoverClosed();
        //        }
        //    }
        //}

    }

    public void SelfDestruct(bool pseudo)
    {
        if (controlledBy)
            ExitControl(controlledBy);

        //var explosion = Instantiate(explosionPrefab,transform.position, Quaternion.identity);
        //var control = explosion.GetComponentInChildren<ExplosionController>();
        //control.explosionDamage = 100;
        if (pseudo)
        {
            Update();   //do single update to forward alls states
            enabled = false;
            Renderer[] r = GetComponentsInChildren<Renderer>();
            foreach (var c in r)
                c.enabled = false;
        }
        else
            Destroy(gameObject);
    }

    private void UpdateStatusConsole()
    {
        try
        {
            statusConsole.Set(StatusProperty.EnergyLevel, currentEnergy);
            statusConsole.Set(StatusProperty.EnergyCapacity, maxEnergy);
            statusConsole.Set(StatusProperty.BatteryDead, batteryDead);
            statusConsole.Set(StatusProperty.PowerOff, powerOff);
            statusConsole.Set(StatusProperty.IsControlled, !!controlledBy);
            statusConsole.Set(StatusProperty.IsBoarded, !!boardedBy);
            statusConsole.Set(StatusProperty.IsOutOfWater, outOfWater);
            statusConsole.Set(StatusProperty.LookRightAxis, lookRightAxis);
            statusConsole.Set(StatusProperty.LookUpAxis, lookUpAxis);
            statusConsole.Set(StatusProperty.ForwardAxis, forwardAxis);
            statusConsole.Set(StatusProperty.RightAxis, rightAxis);
            statusConsole.Set(StatusProperty.UpAxis, upAxis);
            //statusConsole.Set(StatusProperty.OverdriveActive, overdriveActive);
            statusConsole.Set(StatusProperty.CameraDistance, positionCamera.DistanceToTarget);
            statusConsole.Set(StatusProperty.PositionCameraBelowSub, positionCamera.positionBelowTarget);
            statusConsole.Set(StatusProperty.Velocity, rb.velocity.magnitude);
            statusConsole.Set(StatusProperty.FreeCamera, ApplyFreeCamera);
            statusConsole.Set(StatusProperty.TimeDelta, Time.deltaTime);
            statusConsole.Set(StatusProperty.FixedTimeDelta, Time.fixedDeltaTime);
            //statusConsole.Set(StatusProperty.TargetScanTime, scanner.lastScanTime);
            statusConsole.Set(StatusProperty.Health, currentHealth);
            statusConsole.Set(StatusProperty.MaxHealth, maxHealth);
            statusConsole.Set(StatusProperty.IsHealing, isHealing);
            statusConsole.Set(StatusProperty.OnboardingCooldown, OnboardingCooldown);
            statusConsole.Set(StatusProperty.OpenUpgradeCover, openUpgradeCover);
            statusConsole.Set(StatusProperty.IsFirstPerson, positionCamera.isFirstPerson);
            statusConsole.Set(StatusProperty.Lights, lights);
            statusConsole.Set(StatusProperty.NumDockedVehicles, bayControl.NumDockedVehicles);
        }
        catch (Exception ex)
        {
            Debug.LogError(nameof(UpdateStatusConsole));
            Debug.LogException(ex);
        }
    }

    private void UpdateRudders()
    {
        try
        {
            orientation.leanIntensity = doAutoLevel ? 1 : environmentalLeanIntensity;
            orientation.isOutOfWater = outOfWater;
            if (orientation.Intention != null)
            {
                var projection = orientation.Intention.TranslateBy(rb.velocity, isMovingInReverse);
                foreach (var rudder in rudders)
                    rudder.UpdateIntention(projection, isMovingInReverse);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(nameof(UpdateRudders));
            Debug.LogException(ex);
        }

    }

    private void UpdateBay()
    {
        try
        {
            //bayControl.open = openBay;
            bayControl.maxDockedVehicles = maxDockedVehicles;
        }
        catch (Exception ex)
        {
            Debug.LogError(nameof(UpdateBay));
            Debug.LogException(ex);
        }

    }

    private void UpdateFirstPerson()
    {
        try
        {
            firstPersonMarkers.overdriveActive = false;
            firstPersonMarkers.show =
                positionCamera.isFirstPerson
                && IsBeingControlled
                && !batteryDead
                && !powerOff;
            if (firstPersonMarkers.show)
                firstPersonMarkers.transform.position = cameraRoot.position;
        }
        catch (Exception ex)
        {
            Debug.LogError(nameof(UpdateFirstPerson));
            Debug.LogException(ex);
        }

    }

    private void UpdateHealingLights()
    {
        try
        {
            foreach (var h in healingLights)
                h.isHealing = isHealing;
        }
        catch (Exception ex)
        {
            Debug.LogError(nameof(UpdateHealingLights));
            Debug.LogException(ex);
        }

    }

    private void UpdateEnergyLevels()
    {
        try
        {
            energyHistory.Add(currentEnergy);
            var edge = energyHistory.GetEdge();
            if (energyLevel)
            {
                if (edge.HasValue)
                {
                    float energyChange = (currentEnergy - edge.Value) * 5f;
                    energyLevel.currentChange = energyChange;
                }
                else
                    energyLevel.currentChange = 0;

                energyLevel.maxEnergy = maxEnergy;
                energyLevel.currentEnergy = currentEnergy;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(nameof(UpdateEnergyLevels));
            Debug.LogException(ex);
        }
    }

    private void UpdateCameraInCockpit()
    {
        try
        {
            bool cameraCenterIsCockpit = //(zoomedInIsCockpit && positionCamera.isFirstPerson) ||
                forceCockpitCamera;

            if (currentCameraCenterIsCockpit != cameraCenterIsCockpit && currentlyControlled)
            {
                currentCameraCenterIsCockpit = cameraCenterIsCockpit;
                if (currentCameraCenterIsCockpit)
                    MoveCameraOutOfTrailSpace(!currentlyControlled);
                else
                    MoveCameraToTrailSpace();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(nameof(UpdateCameraInCockpit));
            Debug.LogException(ex);
        }
    }

    private void UpdateConsoleVisibility()
    {
        try
        {

            if (Input.GetKeyDown(openConsoleKey))
            {
                if (IsBeingControlled)
                {
                    statusConsole.ToggleVisibility();

                }
                else
                    Log.Write($"Not currently boarded. Ignoring console key");

            }
        }
        catch (Exception ex)
        {
            Debug.LogError(nameof(UpdateConsoleVisibility));
            Debug.LogException(ex);
        }
    }

    private void UpdateCameraAndOrientation()
    {
        try
        {
            rotateCamera.rotationAxisX = lookRightAxis;
            rotateCamera.rotationAxisY = lookUpAxis;
            if (nonCameraOrientation)
            {
                nonCameraOrientation.doAutoLevel = doAutoLevel;
                if (doAutoLevel)
                    nonCameraOrientation.isActive = true;
            }

            positionCamera.positionBelowTarget = positionCameraBelowSub;

            if (currentlyControlled)
            {
                rotateCamera.enabled = true;

                if (ApplyFreeCamera)
                {
                    rotateCamera.AbortTransition();
                    ChangeState(CameraState.IsFree);
                    inWaterDirectionSource = nonCameraOrientation;
                    if (nonCameraOrientation)
                        nonCameraOrientation.isActive = true;
                }
                else
                {

                    switch (state)
                    {
                        case CameraState.IsTransitioningToBound:
                            if (rotateCamera.IsTransitionDone || doAutoLevel)
                            {
                                ChangeState(CameraState.IsBound);

                                inWaterDirectionSource = new TransformDirectionSource(trailSpace);

                                if (nonCameraOrientation)
                                    nonCameraOrientation.isActive = doAutoLevel;
                                rotateCamera.AbortTransition();
                            }
                            break;
                        case CameraState.IsFree:
                            ChangeState(CameraState.IsTransitioningToBound);
                            rotateCamera.BeginTransitionTo(transform);
                            break;

                    }
                }


                if (orientation)
                    orientation.targetOrientation
                        = outOfWater
                            ? fallOrientation
                            : doAutoLevel
                                ? nonCameraOrientation
                                : inWaterDirectionSource;
                if (nonCameraOrientation)
                    nonCameraOrientation.outOfWater = outOfWater;

                if (outOfWater)
                {
                    if (nonCameraOrientation)
                    {
                        nonCameraOrientation.rightRotationSpeed = 0;
                        nonCameraOrientation.upRotationSpeed = 0;
                    }
                }
                else
                {
                    if (nonCameraOrientation)
                    {
                        if (isMovingInReverse && flipFreeVerticalRotationInReverse)
                            nonCameraOrientation.upRotationSpeed = upAxis * rotationDegreesPerSecond;
                        else
                            nonCameraOrientation.upRotationSpeed = -upAxis * rotationDegreesPerSecond;
                        if (isMovingInReverse && flipFreeHorizontalRotationInReverse)
                            nonCameraOrientation.rightRotationSpeed = -rightAxis * rotationDegreesPerSecond;
                        else
                            nonCameraOrientation.rightRotationSpeed = rightAxis * rotationDegreesPerSecond;
                    }
                }




                positionCamera.zoomAxis = zoomAxis;
            }
            else
            {
                if (nonCameraOrientation)
                {
                    nonCameraOrientation.isActive = doAutoLevel;
                    nonCameraOrientation.rightRotationSpeed = 0;
                    nonCameraOrientation.upRotationSpeed = 0;
                }
                //if (isDocked)
                //{
                //    rotateCamera.CopyOrientationFrom(transform);
                //}

                rotateCamera.enabled = false;
                positionCamera.zoomAxis = 0;
                if (orientation)
                    orientation.targetOrientation
                        = outOfWater
                            ? (IDirectionSource)fallOrientation
                            : nonCameraOrientation;

            }

            if (orientation)
            {
                orientation.enabled =
                    (doAutoLevel ||
                        (!IsBoardedButNotControlled && (wasEverBoarded || !outOfWater))
                    )
                    && (
                           (!batteryDead && !powerOff)
                            || doAutoLevel
                        )
                    ;
                orientation.isMovingInReverse = isMovingInReverse;
                orientation.rotationDegreesPerSecond = rotationDegreesPerSecond;
            }

        }
        catch (Exception ex)
        {
            Debug.LogError(nameof(UpdateCameraAndOrientation));
            Debug.LogException(ex);

        }
    }


    private void UpdateDrives()
    {
        try
        {
            if (IsBeingControlled)
            {
                if (outOfWater)
                {
                    backFacingLeft.thrust = 0;
                    backFacingRight.thrust = 0;

                    backFacingLeft.overdrive = 0;
                    backFacingRight.overdrive = 0;
                }
                else
                {
                    backFacingLeft.thrust = forwardAxis + orientation.HorizontalRotationIntent * 0.001f;
                    backFacingRight.thrust = forwardAxis - orientation.HorizontalRotationIntent * 0.001f;


                    //if (overdriveActive)
                    //{
                    //    float overdriveThreshold = 0.5f;
                    //    if (forwardAxis > overdriveThreshold)
                    //    {
                    //        firstPersonMarkers.overdriveActive = true;
                    //        backFacingRight.overdrive =
                    //        backFacingLeft.overdrive =
                    //            (forwardAxis - overdriveThreshold) / (1f - overdriveThreshold);
                    //    }
                    //    else
                    //        backFacingLeft.overdrive = backFacingRight.overdrive = 0;
                    //}
                    //else
                    //    backFacingLeft.overdrive = backFacingRight.overdrive = 0;

                }
            }
            else
            {
                backFacingLeft.thrust = 0;
                backFacingRight.thrust = 0;
            }

            forwardFacingLeft.thrust = -backFacingLeft.thrust;
            forwardFacingRight.thrust = -backFacingRight.thrust;

        }
        catch (Exception ex)
        {
            Debug.LogError(nameof(UpdateDrives));
            Debug.LogException(ex);
        }
    }


    void LateUpdate()
    {
        UpdateFirstPerson();

    }

    void Update()
    {
        try
        {
            MonitorPlayer();

            MonitorPhysics();

            ProcessUpgradeCover();

            UpdateRudders();

            UpdateStatusConsole();

            UpdateBay();

            UpdateHealingLights();

            UpdateEnergyLevels();

            UpdateCameraInCockpit();

            UpdateConsoleVisibility();

            UpdateCameraAndOrientation();

            UpdateDrives();

            UpdateLighting();
        }
        catch (Exception ex)
        {

            Log.LogError(nameof(ArchonControl) + "." + nameof(Update), ex);
        }
    }

    private InteriorLightColor lastLightColor, interpolatingFrom;
    private InteriorLightState lastLightState;
    private void UpdateLighting()
    {
        try
        {
            hullLightController.lightsEnabled = lights;


            InteriorLightState lightState = new InteriorLightState(
                interiorLightsEnabled,
                interiorLightScale,
                batteryDead || powerOff);
            if (lastLightState != lightState)
            {
                lastLightState = lightState;
                interpolatingFrom = lastLightColor;
            }

            var lightColor = M.Gray(interiorLightScale);
            var stripColor = lightColor;
            if (batteryDead || powerOff)
                lightColor = stripColor = new Color(
                    interiorLightScale * (0.3f + 0.3f * Mathf.Sin(Time.time * 3f)),
                    0,
                    0);
            else if (interiorLightsEnabled && reactorIsCharging)
                stripColor = Color.Lerp(stripColor, new Color(0.6f, 0.6f, 1.2f, 1f), 0.5f + 0.5f * Mathf.Sin(Time.time * 3));
            else if (!interiorLightsEnabled)
                stripColor = lightColor = Color.black;

            InteriorLightColor newColor = new InteriorLightColor(stripColor, lightColor);

            lastLightColor = InteriorLightColor.Lerp(
                interpolatingFrom,
                newColor,
                Mathf.Clamp01((Time.time - interpolatingFrom.Recorded) / 1.5f));


            var listeners = GetComponentsInChildren<ILightListener>(true);
            listeners.ForEach(
                listener =>
                {
                    listener.SetInteriorLight(lightColor: lastLightColor.LightColor, stripColor: lastLightColor.StripColor);
                }
                );

        }
        catch (Exception ex)
        {
            Log.LogError(nameof(UpdateLighting), ex);
        }
    }

    public bool BoardedByHeadless => boardedBy && boardedLeave;

    private void MonitorPlayer()
    {
        try
        {
            if (boardedBy)
            {
                bool onLeave = boardedBy.HasDetachedHead;

                if (boardedLeave != onLeave)
                {
                    Log.LogWarning($"Detected leave-change: now {onLeave}");
                    boardedLeave = onLeave;

                    SetRenderAndCollisionActive(interior, !onLeave);
                    SetRenderAndCollisionActive(exterior, onLeave);
                    evacuateIntruders.enabled = !onLeave;
                }

                if (checkFloatingCharacterForSeconds > 0 && !onLeave)
                {
                    var player = boardedBy.Root;
                    //if (player && player.transform && interiorCollider && interiorCollider.enabled)
                    //{
                    //    var hits = Physics.RaycastAll(new Ray(player.transform.position, Vector3.down), 100);
                    //    var hit = hits.Where(h => h.collider == interiorCollider).LeastOrDefault(x => x.distance);
                    //    if (hit.collider && hit.distance > 2)
                    //    {
                    //        var target = hit.point + Vector3.up * 2;
                    //        Log.LogWarning($"Floating character detected. Forcing onboard ({player.transform.position} -> {target}) @{checkFloatingCharacterForSeconds}");
                    //        player.transform.position = target;
                    //        //checkFloatingCharacter = false;
                    //    }
                    //}
                    checkFloatingCharacterForSeconds -= Time.deltaTime;
                }

            }
            if (cameraIsInTrailspace && cameraRoot.parent != trailSpaceCameraContainer)
            {
                Log.LogWarning("Fixing camera location");
                cameraRoot.parent = trailSpaceCameraContainer;
                Location.LocalIdentity.ApplyTo(cameraRoot);
                Log.Write("Fixed");

            }
            if (TrailSpaceShouldBeOffloaded != TrailSpaceIsOffloaded)
            {
                if (TrailSpaceShouldBeOffloaded)
                {
                    Log.LogWarning("Trail space should be offloaded. Offloading");
                    OffloadTrailSpace();
                }
                else
                {
                    Log.LogWarning("Trail space should not be offloaded. Moving back to transform");
                    ReintegrationTrailSpace();
                }
                if (TrailSpaceShouldBeOffloaded != TrailSpaceIsOffloaded)
                    Log.LogError("Trail space offloading failed. Please report this bug");
            }



            hullLightController.lightsEnabled = lights;
        }
        catch (Exception ex)
        {
            Debug.LogError(nameof(MonitorPlayer));
            Debug.LogException(ex);
        }
    }


    private void MonitorPhysics()
    {
        try
        {
            foreach (var c in DisabledColliders)
            {
                if (c.enabled)
                {
                    Log.LogWarning($"Re-disabling collider {c.NiceName()}");
                    c.enabled = false;
                }
            }

            foreach (var c in EnabledColliders)
            {
                if (!c.enabled)
                {
                    Log.LogWarning($"Re-enabling collider {c.NiceName()}");
                    c.enabled = true;
                }
                if (!c.gameObject.activeInHierarchy)
                {
                    Log.LogWarning($"Collider game object {c.gameObject.NiceName()} has been disabled. Fixing");
                    c.gameObject.RequireActive(transform);
                }
                //somehow collisions between the player and interior mesh colliders
                //get disabled when the player aims the build-tool at them.
                //They work fine before that, but after that the player
                //can walk/fall through the interior mesh collider in question.
                //doesn't look like it's actually disabled (the above don't trigger).
                //If the build tool is disabled, the collisions work again.
                //this is very slow and never triggers:
                //if (!c.isTrigger)
                //{
                //    foreach (var collider in PlayerAdapter.Player().transform.GetAllColliders(null))
                //    {
                //        if (!collider.enabled || collider.isTrigger)
                //            continue;
                //        if (Physics.GetIgnoreCollision(c, collider))
                //        {
                //            Log.LogWarning($"Player collision disabled between {c.NiceName()} and {collider.NiceName()}");
                //            Physics.IgnoreCollision(c, collider, false);
                //        }
                //    }
                //}

            }


            if (outOfWater)
            {
                drag.density = 0.01f;
            }
            else
            {
                drag.density = 0.5f;
            }

            forceAutoLevelInSeconds -= Time.deltaTime;
            if (forceAutoLevelInSeconds < 0)
            {
                Log.Write("Force-leveling");
                transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
                forceAutoLevelInSeconds = float.MaxValue;
            }

            rb.CheckIsKinematic(shouldBeKinematic);

            if (rb.drag != 0)
            {
                Log.LogWarning("Re-setting drag to 0");
                rb.drag = 0;
            }
            if (rb.angularDrag != 1)
            {
                Log.LogWarning("Re-setting angular drag to 1");
                rb.angularDrag = 1;
            }

            var forwardSpeed = M.Dot(rb.velocity, transform.forward) + forwardAxis * 100f;
            if (forwardSpeed < -10)
                isMovingInReverse = true;
            else if (forwardSpeed > -5)
                isMovingInReverse = false;

            drag.enabled = !outOfWater;
        }
        catch (Exception ex)
        {
            Debug.LogError(nameof(MonitorPhysics));
            Debug.LogException(ex);
        }
    }

    //public void Localize(Transform player)
    //{
    //    player.parent = helmSeatRoot;
    //    player.localPosition = Vector3.zero;
    //    player.localEulerAngles = Vector3.zero;
    //}

    public void UpdateLowCamera(float oceanY)
    {
        if (transform.position.y >= oceanY - 35 && transform.position.y < oceanY - 1)
            positionCameraBelowSub = true;
        else if (transform.position.y < oceanY - 40 || transform.position.y > oceanY - 2)
            positionCameraBelowSub = false;
    }


    public bool HasSelectedDockable => selectedDockable != null;
    public void SignalDockedChange(IDockable dockable)
    {
        if (selectedDockable == dockable)
            SignalDockableSelectedOrChanged();
    }

    public void UndockSelected()
    {
        if (selectedDockable == null)
        {
            Log.LogWarning("UndockSelected called without selected dockable");
            return;
        }
        bayControl.Undock(selectedDockable.GameObject);
    }

    public void SelectLeft()
    {
        if (selectedDockable == null)
            return;
        var docked = bayControl.Docked.ToList();
        int idx = docked.IndexOf(selectedDockable);
        if (idx < 0)
        {
            selectedDockable = docked.FirstOrDefault();
        }
        else
        {
            selectedDockable = docked[(idx - 1 + docked.Count) % docked.Count];
        }
        SignalDockableSelectedOrChanged();
    }

    public int SelectedDockedIndex =>
        bayControl.Docked.IndexOf(selectedDockable);
    public void SelectRight()
    {
        if (selectedDockable == null)
            return;
        var docked = bayControl.Docked.ToList();
        int idx = docked.IndexOf(selectedDockable);
        if (idx < 0)
        {
            selectedDockable = docked.FirstOrDefault();
        }
        else
        {
            selectedDockable = docked[(idx + 1) % docked.Count];
        }
        SignalDockableSelectedOrChanged();
    }

    private void SignalDockableSelectedOrChanged()
    {
        GetComponentsInChildren<IDockableSelectionListener>(true).ForEach(
            listener => listener.OnDockableSelectedOrChanged(selectedDockable)
            );
    }
    public void SignalDockedChange()
    {
        if (selectedDockable == null)
        {
            selectedDockable = bayControl.Docked.FirstOrDefault();
            SignalDockableSelectedOrChanged();
        }
        else
        {
            if (!bayControl.Docked.Contains(selectedDockable))
            {
                selectedDockable = bayControl.Docked.FirstOrDefault();
                SignalDockableSelectedOrChanged();
            }
        }
    }
}


public enum UndockingCheckResult
{
    Ok,
    Busy,
    NotDocked,
    NotDockable,
    Obstructed,
    DoesNotExist,
}
