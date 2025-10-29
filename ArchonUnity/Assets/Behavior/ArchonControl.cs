using Assets.Behavior.Adapters;
using Assets.Behavior.Components;
using Assets.Behavior.Components.Animations;
using Assets.Behavior.Components.Docking;
using Assets.Behavior.Components.Other;
using Assets.Behavior.Components.Watchdog;
using Assets.Behavior.Interfaces;
using Assets.Behavior.TransferTypes;
using Assets.Behavior.Util;
using Assets.Behavior.Util.Lighting;
using Assets.Behavior.Util.Undoable;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;




public class ArchonControl : MonoBehaviour
{
    [Header("Input Axes")]
    public float forwardAxis;
    public float rightAxis;
    public float upAxis;
    public float zoomAxis;
    public float lookRightAxis;
    public float lookUpAxis;


    [Header("Other Stati")]
    public bool batteryDead;
    public float currentEnergy = 0.5f;
    public float currentHealth = 0.5f;
    public bool doAutoLevel;
    public float engineSoundVolume = 1;
    public float bioreactorSoundVolume = 1;
    public float environmentalLeanIntensity = 1;
    public bool flipFreeHorizontalRotationInReverse = true;
    public bool flipFreeVerticalRotationInReverse = false;
    public bool floodLights;
    public LightShadows floodLightShadows = LightShadows.None;
    public bool forceCockpitCamera;
    public bool freeCameraInCockpit = false;
    public bool freeCameraInExternalCamera = true;
    public float interiorLightScale = 1;
    public bool isHealing;
    private bool isMovingInReverse;
    public int maxDockedVehicles = 2;
    public float maxEnergy = 1;
    public float maxHealth = 1;
    public int minimumInteriorLightPriority;
    public KeyCode openConsoleKey = KeyCode.F7;
    public bool openUpgradeCover;
    public bool openPowerCellCover;
    public bool positionCameraBelowSub;
    public bool powerOff;
    public bool reactorIsCharging;
    public float rotationDegreesPerSecond = 20;
    public float secondsToTeleport = 100;
    public bool showHudMap = true;
    public TeleportationType teleportationType = TeleportationType.None;
    public float teleportationProgress = 0;
    public bool outOfWater;
    public bool zoomedInIsCockpit = true;

    //public Renderer[] onEnterDisableShadows;

    [Header("Linked components")]
    public DriveControl backFacingLeft;
    public DriveControl backFacingRight;
    public BayControl bayControl;
    public Bioreactor bioreactor;
    public Transform dockingTrigger;
    public Transform dockedSpace;
    public Transform exterior;
    public Renderer exteriorInteriorShadowCaster;
    public Transform exteriorModel;
    public DriveControl forwardFacingLeft;
    public DriveControl forwardFacingRight;
    public GameObject[] glass;
    public Transform hangarRoot;
    public HealingLight[] healingLights;
    public HelmSeatController helmSeatController;
    public HudTeleportationAnimation hudTeleportationAnimation;
    public Transform interior;
    public Renderer interiorExteriorShadowCaster;
    public Transform interiorLights;
    public Camera mapCamera;
    public RawImage mapImage;
    public MapControl mapTable;
    public AnimationController powerCellAnimation;
    public PlayerDetector powerCellPlayerDetector;
    public StatusConsole statusConsole;
    public TeleportationAnimation teleportationAnimation;
    public Transform trailSpace;
    public Transform trailSpaceCameraContainer;
    public AnimationController upgradeCoverAnimation;


    public const int VisibleMapLayer = 8;
    public const int InvisibleMapLayer = 27;


    public const int OuterShellLayer = 30;

    //public bool overdriveActive;
    private int effectiveInteriorLightPriority;






    private DateTime lastOnboarded;
    private IDockable selectedDockable;

    private bool boardedLeave;
    private PlayerReference boardedBy,
                    controlledBy;
    private readonly UndoableActions controlUndo = new UndoableActions();
    private readonly FloatTimeFrame energyHistory = new FloatTimeFrame(TimeSpan.FromSeconds(2));

    public bool CameraIsInVehicle { get; private set; }


    private float forceAutoLevelInSeconds = float.MaxValue;

    private FirstPersonMarkers firstPersonMarkers;


    private bool interiorLightsEnabled;
    private EnergyLevel energyLevel;

    private Transform cameraRoot;
    private bool helmCameraIsInVehicle;




    private RotateCamera rotateCamera;
    private PositionCamera positionCamera;
    private NonCameraOrientation nonCameraOrientation;
    private FallOrientation fallOrientation;
    private HullLightController hullLightController;

    private DirectionalDrag drag;
    private DirectAt orientation;
    private RudderControl[] rudders;
    private Rigidbody rb;
    private EvacuateIntruders evacuateIntruders;
    private bool currentlyControlled;

    private Parentage cameraParentageBeforeControl, preBoardedPlayer;
    private Parentage cameraParentageBeforeMoveToTrailspace, inVehicleSeatOrigin;

    private bool currentCameraCenterIsCockpit;
    private bool cameraIsInTrailspace;

    private ColliderWatchdog colliderWatchdog;
    private RigidbodyWatchdog rigidbodyWatchdog;

    private bool wasEverBoarded;

    private bool shouldBeKinematic = true;
    public bool IsBeingControlled => currentlyControlled && !forceCockpitCamera;

    public bool UseFreeCamera => positionCamera.isFirstPerson
        ? freeCameraInCockpit
        : freeCameraInExternalCamera;// || (zoomedInIsCockpit && positionCamera.isFirstPerson));

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


    public void ToggleCurrentFreeCamera()
    {
        if (positionCamera.isFirstPerson)
        {
            freeCameraInCockpit = !freeCameraInCockpit;
        }
        else
        {
            freeCameraInExternalCamera = !freeCameraInExternalCamera;
        }
    }

    private void ChangeState(CameraState state)
    {
        //Debug.Log($"->{state}");
        this.state = state;
    }

    private void MoveCameraToTrailSpace()
    {
        using (var log = Log.New())
        {
            if (!cameraIsInTrailspace)
            {
                cameraIsInTrailspace = true;

                log.Write("Moving camera to trailspace. Setting secondary fallback camera transform");
                SetCameraIsInVehicle(positionCamera.IsInVehicle, false);

                CameraUtil.secondaryFallbackCameraTransform = trailSpaceCameraContainer;

                cameraParentageBeforeMoveToTrailspace = Parentage.FromLocal(cameraRoot);

                log.Write($"Reparenting camera root to trailspace: {trailSpaceCameraContainer.NiceName()}");
                cameraRoot.parent = trailSpaceCameraContainer;
                Location.LocalIdentity.ApplyTo(cameraRoot);



                if (helmCameraIsInVehicle)
                {
                    log.Write($"Helm camera is in vehicle. Updating renderers and colliders");
                    helmCameraIsInVehicle = false;
                    ChangeCameraIsInVehicle(true);
                }


                log.Write("Moved");
            }
        }
    }

    //internal void RestoreCockpitView(Vector3 lookAt)
    //{
    //    MoveCameraOutOfTrailSpace(false);
    //    if (controlledBy)
    //        controlledBy.LookInDirection(lookAt);
    //}

    private readonly Shader glassShader;


    private void SetCollidersEnabled(IEnumerable<Collider> colliders, bool enable)
    {
        colliderWatchdog.Include(colliders, enable);
    }

    private void UpdateInteriorCollidersAndLights(bool enable)
    {
        using (var log = Log.New())
        {
            SetCollidersEnabled(interior.GetAllColliders(PlayerAdapter.Player()), enable);

            interiorLights.gameObject.SetActive(enable);
            glass?.ForEach(g => g.SetActive(enable));
            interiorLightsEnabled = enable;
        }
    }

    private void MoveCameraOutOfTrailSpace(bool withCollidersAndLights)
    {
        using (var log = Log.New())
        {
            if (cameraIsInTrailspace)
            {
                cameraIsInTrailspace = false;

                log.Write("Moving camera out of trailspace. Unsetting secondary fallback camera transform");
                SetCameraIsInVehicle(true, withCollidersAndLights);

                CameraUtil.secondaryFallbackCameraTransform = null;

                log.Write(
                    $"Restoring camera location at {cameraParentageBeforeMoveToTrailspace.Parent.NiceName()}, {cameraParentageBeforeMoveToTrailspace.Transform}");
                cameraParentageBeforeMoveToTrailspace.Restore();


                if (helmCameraIsInVehicle)
                {
                    log.Write($"Restoring helm seat parentage");
                    inVehicleSeatOrigin.Restore();
                }


                log.Write("Moved");
            }
            else
            {
                UpdateInteriorCollidersAndLights(withCollidersAndLights);

            }
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
        using (var log = Log.New())
        {
            if (t)
            {
                foreach (Renderer r in t.GetComponentsInChildren<Renderer>())
                {
                    r.enabled = active;
                }

                SetCollisionActive(t, active);
            }
        }

    }

    public void Undock(GameObject dockedSub)
    {
        bayControl.Undock(dockedSub);
    }

    /// <summary>
    /// Updates the camera to first or third person.
    /// </summary>
    /// <param name="firstPerson"></param>
    public void SetCameraFirstPerson(bool firstPerson)
    {
        if (!positionCamera)
            positionCamera = trailSpace.GetComponent<PositionCamera>();
        positionCamera.SetCameraFirstPerson(firstPerson);
    }

    public UndockingCheckResult CheckUndocking(GameObject dockedSub)
    {
        return bayControl.CheckUndocking(dockedSub);
    }


    public void SignalTeleported()
    {
        if (nonCameraOrientation)
        {
            nonCameraOrientation.SignalTeleported();
        }

        teleportationAnimation.SignalTeleported();
    }

    public void SignalLoading()
    {
        bayControl.SignalLoading();
    }


    public void RedetectDocked()
    {
        _ = bayControl.RedetectDocked();
    }

    public void PrepareForSaving()
    {
        using (var log = Log.New())
            bayControl.PrepareForSaving();
    }

    public void Enter(PlayerReference player, bool skipOrientation = false)
    {
        using (var log = Log.New())
        {
            log.Write($"Boarding ({player}, {skipOrientation})");
            RigidbodyUtil.SetKinematic(rb);
            shouldBeKinematic = true;
            transform.eulerAngles = M.V3(0, transform.localEulerAngles.y, 0);

            forceAutoLevelInSeconds = !skipOrientation ? 0 : float.MaxValue;

            checkFloatingCharacterForSeconds = 1;
            boardedBy = player;
            boardedLeave = false;
            //mapTable.gameObject.SetActive(true);
            //mapHud.gameObject.SetActive(false);

            helmSeatController.AnimateToParkPosition();
            SetCameraIsInVehicle(true, true);
            mapTable.gameObject.SetActive(true);
            mapTable.mapLayer = VisibleMapLayer;
            mapTable.upClip = 0.253f;
            mapTable.downClip = -2.02f;

        }
        //evacuateIntruders.enabled = true;
    }

    internal void ChangeCameraIsInVehicle(bool isInVehicle)
    {
        using (var log = Log.New())
        {
            if (isInVehicle && !helmCameraIsInVehicle)
            {
                if (helmSeatController.transform.parent != trailSpaceCameraContainer)
                {
                    log.Write(
                        $"Camera is in vehicle but helm camera was not previously. Recording vehicle seat origin then reparenting");

                    inVehicleSeatOrigin = helmSeatController.Reparent(trailSpaceCameraContainer);
                }
                else
                {
                    log.Error(
                        $"Helm seat controller is already in trail space camera container. This should not happen. Parent: {helmSeatController.transform.parent.NiceName()}");
                }
                //if (helmSeatController.transform.parent != trailSpaceCameraContainer)
                //{}
            }
            else if (!isInVehicle && helmCameraIsInVehicle)
            {
                log.Write(
                    $"Camera is no longer in vehicle but helm camera was previously. Restoring vehicle seat origin");
                inVehicleSeatOrigin.Restore();
            }

            SetCameraIsInVehicle(isInVehicle, false);



            helmCameraIsInVehicle = isInVehicle;
            if (helmCameraIsInVehicle)
            {
                ReintegrateTrailSpace();
            }
            else
            {
                OffloadTrailSpace();
            }

        }
    }


    internal Vector3 GetHelmCameraCenter()
    {
        return helmCameraIsInVehicle
            ? transform.TransformPoint(inVehicleSeatOrigin.Transform.Position)
            : helmSeatController.transform.position;
    }

    private void SetCameraIsInVehicle(bool isInVehicle, bool withCollidersAndLights)
    {
        using (var log = Log.New())
        {

            log.Write(
                $"Setting camera is in vehicle: {isInVehicle}, withCollidersAndLights = {withCollidersAndLights}");
            CameraIsInVehicle = isInVehicle;

            interior.GetAll<Renderer>(PlayerAdapter.Player()).ForEach(r =>
            {
                r.enabled = isInVehicle;
            });

            interior.GetAll<PowerCellRoot>(PlayerAdapter.Player()).ForEach(r =>
            {
                r.gameObject.SetActive(isInVehicle);
                log.Debug($"Setting power cell root {r.NiceName()} active = {isInVehicle}");
            });

            //interior.gameObject.SetActive(isInVehicle);
            UpdateInteriorCollidersAndLights(isInVehicle && withCollidersAndLights);

            exteriorInteriorShadowCaster.enabled = isInVehicle;
            interiorExteriorShadowCaster.enabled = isInVehicle;
            if (exteriorModel)
            {
                exteriorModel.GetComponentsInChildren<Renderer>().ForEach(c =>
                    c.shadowCastingMode = isInVehicle
                        ? UnityEngine.Rendering.ShadowCastingMode.Off
                        : UnityEngine.Rendering.ShadowCastingMode.On);
            }

            var seatHoverEngine = GetComponentInChildren<SeatEngine>();
            if (seatHoverEngine)
                seatHoverEngine.enabled = isInVehicle;

            log.Write($"Setting exterior colliders to {!isInVehicle || !withCollidersAndLights}");
            SetRenderAndCollisionActive(exterior, !isInVehicle || !withCollidersAndLights);
            SetCollisionActive(helmSeatController.transform, isInVehicle && withCollidersAndLights);
        }
    }

    public void Exit()
    {
        using (var log = Log.New())
        {
            log.Write($"Offboarding");

            SetCameraIsInVehicle(false, false);

            evacuateIntruders.enabled = false;
            checkFloatingCharacterForSeconds = 0;
            if (boardedBy)
            {
                boardedBy = default;
                //RigidbodyUtil.UnsetKinematic(rb);
                //shouldBeKinematic = false;
            }
            //mapTable.gameObject.SetActive(false);
            //mapHud.gameObject.SetActive(false);


            helmSeatController.MoveToControlPosition();
            mapTable.gameObject.SetActive(false);
        }
    }


    public void Control(PlayerReference player)
    {
        using (var log = Log.New())
        {
            RigidbodyUtil.UnsetKinematic(rb);
            shouldBeKinematic = false;

            wasEverBoarded = true;
            checkFloatingCharacterForSeconds = 0;
            lastOnboarded = DateTime.Now;
            forceAutoLevelInSeconds = float.MaxValue;
            if (!currentlyControlled)
            {
                log.Write($"Controlling");

                controlledBy = player;
                Exit();

                mapTable.gameObject.SetActive(true);
                mapTable.mapLayer = InvisibleMapLayer;
                mapTable.upClip = 0.2f;
                mapTable.downClip = -5f;


                BoardingListeners listeners = BoardingListeners.Of(this, trailSpace);

                listeners.SignalEnterControlBegin();

                cameraRoot = player.CameraRoot;
                if (!cameraRoot)
                {
                    cameraRoot = Camera.main.transform;
                }

                log.Write($"Setting {cameraRoot} as cameraRoot");
                CameraUtil.primaryFallbackCameraTransform = cameraRoot;
                cameraParentageBeforeControl = Parentage.FromLocal(cameraRoot);
                //mapTable.gameObject.SetActive(false);
                //mapHud.gameObject.SetActive(showHudMap);

                helmSeatController.MoveToControlPosition();
                preBoardedPlayer = helmSeatController.SeatPlayer(player);
                cameraIsInTrailspace = false; //just in case
                                              //if (!zoomedInIsCockpit || !positionCamera.isFirstPerson)
                MoveCameraToTrailSpace();
                //else
                //    SetCameraIsInVehicle(true, false);
                if (!helmCameraIsInVehicle)
                {
                    OffloadTrailSpace();
                }

                currentlyControlled = true;

                player.DisableCollidersAndRigidbodies(controlUndo);

                listeners.SignalEnterControlEnd();

                if (!UseFreeCamera)
                {
                    ChangeState(CameraState.IsBound);
                    rotateCamera.CopyOrientationFrom(transform);
                }

            }

        }
    }
    private void OffloadTrailSpace()
    {
        using (var log = Log.New())
        {
            if (trailSpace.parent != transform.parent)
            {
                log.Write($"Offloading trail space");
                trailSpace.parent = transform.parent;
            }
        }

    }

    private void ReintegrateTrailSpace()
    {
        using (var log = Log.New())
        {
            if (trailSpace.parent != transform)
            {
                log.Write($"Reintegrating trail space");
                trailSpace.parent = transform;
            }
        }
    }

    private bool TrailSpaceShouldBeOffloaded
        => currentlyControlled && !helmCameraIsInVehicle;
    private bool TrailSpaceIsOffloaded
        => trailSpace.parent == transform.parent;

    public bool ExitControl(PlayerReference player, bool skipOrientation = false)
    {
        using (var log = Log.New())
        {
            if (currentlyControlled && !outOfWater)
            {

                log.Write($"Exiting control");


                controlUndo.UndoAndClear();
                BoardingListeners listeners = BoardingListeners.Of(this, trailSpace);
                try
                {

                    listeners.SignalExitControlBegin();

                    MoveCameraOutOfTrailSpace(true);
                    log.Write($"Restoring parentage");
                    cameraParentageBeforeControl.Restore();
                }
                finally
                {
                    currentlyControlled = false;
                    ReintegrateTrailSpace();
                }

                controlledBy = default;
                Enter(player, skipOrientation);
                preBoardedPlayer.Restore();
                helmSeatController.MoveToParkPosition();
                listeners.SignalExitControlEnd();
                return true;
            }

            return false;
        }
    }

    private void Awake()
    {
        colliderWatchdog = GetComponent<ColliderWatchdog>();
        rigidbodyWatchdog = GetComponent<RigidbodyWatchdog>();
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
    }

    // Start is called before the first frame update
    private void Start()
    {
        using (var log = Log.New())
        {
            //bayControl = GetComponentInChildren<BayControl>();
            if (orientation)
            {
                orientation.targetOrientation = inWaterDirectionSource = new TransformDirectionSource(trailSpace);
            }

            evacuateIntruders.enabled = IsBoardedButNotControlled;
            SetCameraIsInVehicle(false, false);
            mapTable.gameObject.SetActive(false);
        }
    }

    private static string TN(RenderTexture rt)
    {
        return rt == null ? "null" : $"{rt.name}, ptr = {rt.GetNativeTexturePtr()}";
    }


    private static string AllMessages(Exception ex)
    {
        string rs = ex.Message;
        if (ex.InnerException != null)
        {
            rs += "<-" + AllMessages(ex.InnerException);
        }

        return rs;
    }


    private IDirectionSource inWaterDirectionSource;

    public ArchonControl()
    {
    }

    private bool OnboardingCooldown => DateTime.Now - lastOnboarded < TimeSpan.FromSeconds(1);

    private void ProcessCovers()
    {
        try
        {
            upgradeCoverAnimation.animateTowardsEnd = openUpgradeCover;
            powerCellAnimation.animateTowardsEnd = openPowerCellCover || powerCellPlayerDetector.HasPlayer;
        }
        catch (Exception ex)
        {
            using (var log = Log.New())
            {
                log.Error(nameof(ProcessCovers), ex);
            }
        }
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
        {
            _ = ExitControl(controlledBy);
        }

        //var explosion = Instantiate(explosionPrefab,transform.position, Quaternion.identity);
        //var control = explosion.GetComponentInChildren<ExplosionController>();
        //control.explosionDamage = 100;
        if (pseudo)
        {
            Update();   //do single update to forward alls states
            enabled = false;
            Renderer[] r = GetComponentsInChildren<Renderer>();
            foreach (Renderer c in r)
            {
                c.enabled = false;
            }
        }
        else
        {
            Destroy(gameObject);
        }
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
            statusConsole.Set(StatusProperty.ReactorIsCharging, reactorIsCharging);
            statusConsole.Set(StatusProperty.CameraDistance, positionCamera.DistanceToTarget);
            statusConsole.Set(StatusProperty.PositionCameraBelowSub, positionCamera.positionBelowTarget);
            statusConsole.Set(StatusProperty.Velocity, rb.velocity.magnitude);
            statusConsole.Set(StatusProperty.FreeCamera, UseFreeCamera);
            statusConsole.Set(StatusProperty.TimeDelta, Time.deltaTime);
            statusConsole.Set(StatusProperty.FixedTimeDelta, Time.fixedDeltaTime);
            statusConsole.Set(StatusProperty.Health, currentHealth);
            statusConsole.Set(StatusProperty.MaxHealth, maxHealth);
            statusConsole.Set(StatusProperty.IsHealing, isHealing);
            statusConsole.Set(StatusProperty.OnboardingCooldown, OnboardingCooldown);
            statusConsole.Set(StatusProperty.OpenUpgradeCover, openUpgradeCover);
            statusConsole.Set(StatusProperty.IsFirstPerson, positionCamera.isFirstPerson);
            statusConsole.Set(StatusProperty.Lights, floodLights);
            statusConsole.Set(StatusProperty.MaxDockedVehicles, maxDockedVehicles);
            statusConsole.Set(StatusProperty.NumDockedVehicles, bayControl.NumDockedVehicles);
            statusConsole.Set(StatusProperty.HudMapEnabled, showHudMap);
            statusConsole.Set(StatusProperty.AutoLeveling, doAutoLevel);
        }
        catch (Exception ex)
        {
            using (var log = Log.New())
            {

                log.Error(nameof(UpdateStatusConsole), ex);
            }
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
                ProjectedMotionSpace projection = orientation.Intention.TranslateBy(rb.velocity, isMovingInReverse);
                foreach (RudderControl rudder in rudders)
                {
                    rudder.UpdateIntention(projection, isMovingInReverse);
                }
            }
        }
        catch (Exception ex)
        {
            using (var log = Log.New())
            {

                log.Error(nameof(UpdateRudders), ex);
            }
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
            using (var log = Log.New())
            {
                log.Error(nameof(UpdateBay), ex);
            }
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
            {
                firstPersonMarkers.transform.position = cameraRoot.position;
            }
        }
        catch (Exception ex)
        {
            using (var log = Log.New())
            {
                log.Error(nameof(UpdateFirstPerson), ex);
            }
        }

    }

    private void UpdateHealingLights()
    {
        try
        {
            foreach (HealingLight h in healingLights)
            {
                h.isHealing = isHealing;
            }
        }
        catch (Exception ex)
        {
            using (var log = Log.New())
            {
                log.Error(nameof(UpdateHealingLights), ex);
            }
        }

    }

    private void UpdateEnergyLevels()
    {
        try
        {
            energyHistory.Add(currentEnergy);
            float? edge = energyHistory.GetEdge();
            if (energyLevel)
            {
                if (edge.HasValue)
                {
                    float energyChange = (currentEnergy - edge.Value) * 5f;
                    energyLevel.currentChange = energyChange;
                }
                else
                {
                    energyLevel.currentChange = 0;
                }

                energyLevel.maxEnergy = maxEnergy;
                energyLevel.currentEnergy = currentEnergy;
            }
        }
        catch (Exception ex)
        {
            using (var log = Log.New())
                log.Error(nameof(UpdateEnergyLevels), ex);
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
                using (var log = Log.New())
                    log.Write($"Forcing camera into vehicle cockpit: {cameraCenterIsCockpit}");
                currentCameraCenterIsCockpit = cameraCenterIsCockpit;
                if (currentCameraCenterIsCockpit)
                {
                    MoveCameraOutOfTrailSpace(!currentlyControlled);
                }
                else
                {
                    MoveCameraToTrailSpace();
                }
            }
        }
        catch (Exception ex)
        {
            using (var log = Log.New())
                log.Error(nameof(UpdateCameraInCockpit), ex);
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
                {
                    using (var log = Log.New())
                        log.Write($"Not currently boarded. Ignoring console key");
                }
            }
        }
        catch (Exception ex)
        {
            using (var log = Log.New())
                log.Error(nameof(UpdateConsoleVisibility), ex);
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
                {
                    nonCameraOrientation.isActive = true;
                }
            }

            positionCamera.positionBelowTarget = positionCameraBelowSub;

            if (currentlyControlled)
            {
                rotateCamera.enabled = true;

                if (UseFreeCamera)
                {
                    rotateCamera.AbortTransition();
                    ChangeState(CameraState.IsFree);
                    inWaterDirectionSource = nonCameraOrientation;
                    if (nonCameraOrientation)
                    {
                        nonCameraOrientation.isActive = true;
                    }
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
                                {
                                    nonCameraOrientation.isActive = doAutoLevel;
                                }

                                rotateCamera.AbortTransition();
                            }
                            break;
                        case CameraState.IsFree:
                            ChangeState(CameraState.IsTransitioningToBound);
                            rotateCamera.BeginTransitionTo(transform);
                            break;
                        case CameraState.IsBound:
                            break;
                        default:
                            break;
                    }
                }


                if (orientation)
                {
                    orientation.targetOrientation
                        = outOfWater
                            ? fallOrientation
                            : doAutoLevel
                                ? nonCameraOrientation
                                : inWaterDirectionSource;
                }

                if (nonCameraOrientation)
                {
                    nonCameraOrientation.outOfWater = outOfWater;
                }

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
                        nonCameraOrientation.upRotationSpeed = isMovingInReverse && flipFreeVerticalRotationInReverse
                            ? upAxis * rotationDegreesPerSecond
                            : -upAxis * rotationDegreesPerSecond;
                        nonCameraOrientation.rightRotationSpeed = isMovingInReverse && flipFreeHorizontalRotationInReverse
                            ? -rightAxis * rotationDegreesPerSecond
                            : rightAxis * rotationDegreesPerSecond;
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
                {
                    orientation.targetOrientation
                        = outOfWater
                            ? (IDirectionSource)fallOrientation
                            : nonCameraOrientation;
                }
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
            using (var log = Log.New())
                log.Error(nameof(UpdateCameraAndOrientation), ex);

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
                    backFacingLeft.thrust = forwardAxis + (orientation.HorizontalRotationIntent * 0.001f);
                    backFacingRight.thrust = forwardAxis - (orientation.HorizontalRotationIntent * 0.001f);


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
            using (var log = Log.New())
                log.Error(nameof(UpdateDrives), ex);
        }
    }

    private void LateUpdate()
    {
        UpdateFirstPerson();

    }

    private void Update()
    {
        try
        {
            MonitorPlayer();

            MonitorPhysics();

            ProcessCovers();

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

            UpdateTeleportation();

            UpdateBioreactor();

            UpdateHudMap();
        }
        catch (Exception ex)
        {
            using (var log = Log.New())
                log.Error(nameof(ArchonControl) + "." + nameof(Update), ex);
        }
    }

    private void UpdateHudMap()
    {
        try
        {
            bool showHudMap = IsBeingControlled && this.showHudMap;
            if (mapCamera)
                mapCamera.enabled = showHudMap;
            if (mapImage)
                mapImage.enabled = showHudMap;
        }
        catch (Exception ex)
        {
            using (var log = Log.New())
                log.Error(nameof(UpdateHudMap), ex);
        }
    }

    private void UpdateBioreactor()
    {
        try
        {
            if (bioreactor)
            {
                bioreactor.isCharging = OneSecondAccumulator.Average.IsCharging;
                bioreactor.powerOff = batteryDead || powerOff;
                bioreactor.soundVolume = bioreactorSoundVolume;
            }
        }
        catch (Exception ex)
        {
            using (var log = Log.New())
                log.Error(nameof(UpdateBioreactor), ex);
        }
    }

    private void UpdateTeleportation()
    {
        teleportationAnimation.type = teleportationType;
        teleportationAnimation.secondsToTeleport = secondsToTeleport;
        hudTeleportationAnimation.type = teleportationType;
        hudTeleportationAnimation.progress = teleportationProgress;
    }

    //private InteriorLightColor setLightColor, interpolatingFrom, lastLightColor;



    private InteriorLightColor lastLight;
    private LightStateAccumulator OneSecondAccumulator { get; } = new LightStateAccumulator();
    private void UpdateLighting()
    {
        try
        {
            hullLightController.lightsEnabled = floodLights;
            hullLightController.shadows = CameraIsInVehicle ? LightShadows.None : floodLightShadows;

            if (!OneSecondAccumulator.Add(new CapturedLightState(
                interiorLightsEnabled,
                interiorLightScale,
                batteryDead,
                reactorIsCharging)))
                return;

            var avg = OneSecondAccumulator.Average;

            InteriorLightColor newColor = InteriorLightBuilder.Build(avg);

            if (newColor == lastLight && effectiveInteriorLightPriority == minimumInteriorLightPriority)
                return;

            lastLight = newColor;
            effectiveInteriorLightPriority = minimumInteriorLightPriority;
            ILightListener[] listeners = GetComponentsInChildren<ILightListener>(true);
            listeners.ForEach(
                listener =>
                {
                    listener.SetInteriorLight(lightColor: newColor.LightColor, stripColor: newColor.StripColor, minimumInteriorLightPriority: effectiveInteriorLightPriority);
                }
                );

        }
        catch (Exception ex)
        {
            using (var log = Log.New())
                log.Error(nameof(UpdateLighting), ex);
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
                    using (var log = Log.New())
                        log.Warn($"Detected leave-change: now {onLeave}");
                    boardedLeave = onLeave;

                    SetRenderAndCollisionActive(interior, !onLeave);
                    SetRenderAndCollisionActive(exterior, onLeave);
                    evacuateIntruders.enabled = !onLeave;
                }

                if (checkFloatingCharacterForSeconds > 0 && !onLeave)
                {
                    //GameObject player = boardedBy.Root;
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
                using (var log = Log.New())
                {
                    log.Warn("Fixing camera location");
                    cameraRoot.parent = trailSpaceCameraContainer;
                    Location.LocalIdentity.ApplyTo(cameraRoot);
                    log.Write("Fixed");
                }

            }
            if (TrailSpaceShouldBeOffloaded != TrailSpaceIsOffloaded)
            {
                using (var log = Log.New())
                {
                    if (TrailSpaceShouldBeOffloaded)
                    {
                        log.Warn("Trail space should be offloaded. Offloading");
                        OffloadTrailSpace();
                    }
                    else
                    {
                        log.Warn("Trail space should not be offloaded. Moving back to transform");
                        ReintegrateTrailSpace();
                    }
                    if (TrailSpaceShouldBeOffloaded != TrailSpaceIsOffloaded)
                    {
                        log.Error("Trail space offloading failed. Please report this bug");
                    }
                }
            }



            hullLightController.lightsEnabled = floodLights;
        }
        catch (Exception ex)
        {
            using (var log = Log.New())
                log.Error(nameof(MonitorPlayer), ex);
        }
    }


    private void MonitorPhysics()
    {
        try
        {
            drag.density = outOfWater ? 0.01f : 0.5f;

            forceAutoLevelInSeconds -= Time.deltaTime;
            if (forceAutoLevelInSeconds < 0)
            {
                using (var log = Log.New())
                    log.Write("Force-leveling");
                transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
                forceAutoLevelInSeconds = float.MaxValue;
            }

            rb.CheckIsKinematic(shouldBeKinematic && !outOfWater && (wasEverBoarded || bayControl.NumDockedVehicles > 0));

            if (rb.drag != 0)
            {
                using (var log = Log.New())
                    log.Warn("Re-setting drag to 0");
                rb.drag = 0;
            }
            if (rb.angularDrag != 1)
            {
                using (var log = Log.New())
                    log.Warn("Re-setting angular drag to 1");
                rb.angularDrag = 1;
            }

            float forwardSpeed = M.Dot(rb.velocity, transform.forward) + (forwardAxis * 100f);
            if (forwardSpeed < -10)
            {
                isMovingInReverse = true;
            }
            else if (forwardSpeed > -5)
            {
                isMovingInReverse = false;
            }

            drag.enabled = !outOfWater;
        }
        catch (Exception ex)
        {
            using (var log = Log.New())
                log.Error(nameof(MonitorPhysics), ex);
        }
    }

    public void UpdateLowCamera(float oceanY)
    {
        if (transform.position.y >= oceanY - 35 && transform.position.y < oceanY - 1)
        {
            positionCameraBelowSub = true;
        }
        else if (transform.position.y < oceanY - 40 || transform.position.y > oceanY - 2)
        {
            positionCameraBelowSub = false;
        }
    }

    public IDockable SelectedDockable => selectedDockable;
    public bool HasSelectedDockable => selectedDockable != null;
    public void SignalDockedChange(IDockable dockable)
    {
        if (selectedDockable == dockable)
        {
            SignalDockableSelectedOrChanged();
        }
    }

    public void UndockSelected()
    {
        using (var log = Log.New())
        {
            if (selectedDockable == null)
            {
                log.Warn("UndockSelected called without selected dockable");
                return;
            }

            bayControl.Undock(selectedDockable.GameObject);
        }
    }

    public void SelectLeft()
    {
        using (var log = Log.New())
        {
            if (selectedDockable == null)
            {
                log.Warn("SelectLeft called without selected dockable");
                return;
            }


            List<IDockable> docked = bayControl.Docked.ToList();
            log.Write($"Navigating left among {docked.Count} undockable(s)");
            foreach (var dockable in docked)
            {
                log.Write($" - {dockable.Name}");
            }
            int idx = docked.IndexOf(selectedDockable);
            selectedDockable = idx < 0 ? docked.FirstOrDefault() : docked[(idx - 1 + docked.Count) % docked.Count];
            log.Write($"Selected {selectedDockable?.Name}");
            SignalDockableSelectedOrChanged();
        }
    }

    public int SelectedDockedIndex =>
        bayControl.Docked.IndexOf(selectedDockable);
    public void SelectRight()
    {
        using (var log = Log.New())
        {

            if (selectedDockable == null)
            {
                log.Warn("SelectLeft called without selected dockable");
                return;
            }

            List<IDockable> docked = bayControl.Docked.ToList();
            log.Write($"Navigating right among {docked.Count} undockable(s)");
            foreach (var dockable in docked)
            {
                log.Write($" - {dockable.Name}");
            }
            int idx = docked.IndexOf(selectedDockable);
            selectedDockable = idx < 0 ? docked.FirstOrDefault() : docked[(idx + 1) % docked.Count];
            log.Write($"Selected {selectedDockable?.Name}");
            SignalDockableSelectedOrChanged();
        }
    }

    private void SignalDockableSelectedOrChanged()
    {
        using (var log = Log.New())
        {
            GetComponentsInChildren<IDockableSelectionListener>(true)
                .ForEach(listener => listener.OnDockableSelectedOrChanged(selectedDockable)
                );
        }
    }

    internal void SignalDocked(IDockable dockable)
    {
        using (var log = Log.New())
        {
            selectedDockable = dockable;
            log.Write($"Selected {selectedDockable?.Name}");
            SignalDockableSelectedOrChanged();
        }
    }
    public void SignalDockedChange()
    {
        using (var log = Log.NewLazy())
        {
            if (selectedDockable == null)
            {
                selectedDockable = bayControl.Docked.FirstOrDefault();
                log.Write($"Selected {selectedDockable?.Name}");
                SignalDockableSelectedOrChanged();
            }
            else
            {
                if (!bayControl.Docked.Contains(selectedDockable))
                {
                    selectedDockable = bayControl.Docked.FirstOrDefault();
                    log.Write($"Selected {selectedDockable?.Name}");
                    SignalDockableSelectedOrChanged();
                }
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
