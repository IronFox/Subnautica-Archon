using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.LowLevel;
using static UnityEngine.GraphicsBuffer;

public class ArchonControl : MonoBehaviour
{
    public KeyCode openConsoleKey = KeyCode.F7;

    public Transform interior;
    public Transform exterior;
    public Transform controlExit;
    public Transform dockingTrigger;
    public Transform dockedSpace;
    public Transform hangarRoot;

    public float forwardAxis;
    public float rightAxis;
    public float upAxis;
    public float zoomAxis;
    public float lookRightAxis;
    public float lookUpAxis;
    public bool isMovingInReverse;

    private const int OuterShellLayer = 30;

    public bool overdriveActive;
    public bool outOfWater;
    public bool freeCamera;

    public bool isAutoLeveling;

    public bool positionCameraBelowSub;

    public bool cameraCenterIsCockpit;
    public bool powerOff;
    public bool batteryDead;
    public bool openUpgradeCover;
    public bool openBay;
    public bool lights;

    public int maxDockedVehicles = 2;

    private DateTime lastOnboarded;

    private bool boardedLeave;
    private PlayerReference boardedBy,
                    controlledBy;
    private readonly FloatTimeFrame energyHistory = new FloatTimeFrame(TimeSpan.FromSeconds(2));
    public float maxEnergy=1;
    public float currentEnergy=0.5f;
    public float maxHealth = 1;
    public float currentHealth = 0.5f;
    public bool isHealing;

    private FirstPersonMarkers firstPersonMarkers;

    public float rotationDegreesPerSecond = 20;

    private EnergyLevel energyLevel;

    private Transform cameraRoot;

    public DriveControl forwardFacingLeft;
    public DriveControl backFacingLeft;
    public DriveControl forwardFacingRight;
    public DriveControl backFacingRight;

    public HealingLight[] healingLights;

    public Transform trailSpace;
    public Transform trailSpaceCameraContainer;
    public Transform seat;
    public StatusConsole statusConsole;

    private RotateCamera rotateCamera;
    private PositionCamera positionCamera;
    private NonCameraOrientation nonCameraOrientation;
    private FallOrientation fallOrientation;
    private BayControl bayControl;
    private HullLightController hullLightController;

    private DirectionalDrag drag;
    private DirectAt orientation;
    private RudderControl[] rudders;
    private Rigidbody rb;
    private EvacuateIntruders evacuateIntruders;
    private bool currentlyControlled;

    private Parentage onboardLocalizedTransform;
    private Parentage cameraMove;

    private bool currentCameraCenterIsCockpit;
    private bool cameraIsInTrailspace;

    private bool wasEverBoarded;

    public bool IsBeingControlled => currentlyControlled;

    public LogConfig Log { get; } = LogConfig.Default;
    private enum CameraState
    {
        IsFree,
        IsBound,
        IsTransitioningToBound
    }

    private CameraState state = CameraState.IsBound;


    public bool IsBoarded => boardedBy.IsSet;

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
            
            CameraUtil.secondaryFallbackCameraTransform = trailSpaceCameraContainer;

            cameraMove = Parentage.FromLocal(cameraRoot);
            cameraRoot.parent = trailSpaceCameraContainer;
            TransformDescriptor.LocalIdentity.ApplyTo(cameraRoot);
            Log.Write("Moved");
        }
    }

    private void MoveCameraOutOfTrailSpace()
    {
        if (cameraIsInTrailspace)
        {
            cameraIsInTrailspace = false;

            Log.Write("Moving camera out of trailspace. Unsetting secondary fallback camera transform");
            
            CameraUtil.secondaryFallbackCameraTransform = null;

            cameraMove.Restore();
            Log.Write("Moved");
        }
    }


    private static void SetRenderAndCollisionActive(Transform t, bool active)
    {
        if (t)
        {
            foreach (var r in t.GetComponentsInChildren<Renderer>())
                r.enabled = active;
            foreach (var r in t.GetComponentsInChildren<Collider>())
                r.enabled = active;
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


    public void Enter(PlayerReference player)
    {
        Log.Write($"Boarding");
        RigidbodyUtil.SetKinematic(rb);

        transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);

        boardedBy = player;
        boardedLeave = false;

        SetRenderAndCollisionActive(interior, true);
        SetRenderAndCollisionActive(exterior, false);
        evacuateIntruders.enabled = true;
    }

    public void Exit()
    {
        Log.Write($"Offboarding");

        SetRenderAndCollisionActive(interior, false);
        SetRenderAndCollisionActive(exterior, true);
        evacuateIntruders.enabled = false;

        if (boardedBy)
        {
            boardedBy = default;
            RigidbodyUtil.UnsetKinematic(rb);
        }
    }


    public void Control(PlayerReference player)
    {
        wasEverBoarded = true;
        lastOnboarded = DateTime.Now;
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
            if (!currentCameraCenterIsCockpit)
                MoveCameraToTrailSpace();

            Log.Write($"Offloading trail space");
            trailSpace.parent = transform.parent;

            currentlyControlled = true;

            listeners.SignalEnterControlEnd();
        }
    }



    public void ExitControl(PlayerReference player, bool intoShip=true)
    {
        if (currentlyControlled)
        {
            Log.Write($"Exiting control");
            var listeners = BoardingListeners.Of(this, trailSpace);
            try
            {

                listeners.SignalExitControlBegin();

                MoveCameraOutOfTrailSpace();
                Log.Write($"Restoring parentage");
                onboardLocalizedTransform.Restore();
            }
            finally
            {
                currentlyControlled = false;
                Log.Write($"Reintegration trail space");
                trailSpace.parent = transform;
            }
            controlledBy = default;
            if (intoShip)
                Enter(player);
            listeners.SignalExitControlEnd();
        }
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
        bayControl = GetComponentInChildren<BayControl>();
        if (orientation)
            orientation.targetOrientation = inWaterDirectionSource = new TransformDirectionSource(trailSpace);
        evacuateIntruders.enabled = IsBoarded;
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
    private bool coverWasOpen = true;   //call SignalCoverClosed() at start

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
            statusConsole.Set(StatusProperty.OverdriveActive, overdriveActive);
            statusConsole.Set(StatusProperty.CameraDistance, positionCamera.DistanceToTarget);
            statusConsole.Set(StatusProperty.PositionCameraBelowSub, positionCamera.positionBelowTarget);
            statusConsole.Set(StatusProperty.Velocity, rb.velocity.magnitude);
            statusConsole.Set(StatusProperty.FreeCamera, freeCamera);
            statusConsole.Set(StatusProperty.TimeDelta, Time.deltaTime);
            statusConsole.Set(StatusProperty.FixedTimeDelta, Time.fixedDeltaTime);
            //statusConsole.Set(StatusProperty.TargetScanTime, scanner.lastScanTime);
            statusConsole.Set(StatusProperty.Health, currentHealth);
            statusConsole.Set(StatusProperty.MaxHealth, maxHealth);
            statusConsole.Set(StatusProperty.IsHealing, isHealing);
            statusConsole.Set(StatusProperty.OnboardingCooldown, OnboardingCooldown);
            statusConsole.Set(StatusProperty.OpenUpgradeCover, openUpgradeCover);
            statusConsole.Set(StatusProperty.IsFirstPerson, positionCamera.isFirstPerson);
            statusConsole.Set(StatusProperty.OpenBay, openBay);
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
            orientation.rotateZ = !outOfWater;
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
                && currentlyControlled
                && !batteryDead
                && !powerOff;
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
            if (currentCameraCenterIsCockpit != cameraCenterIsCockpit && currentlyControlled)
            {
                currentCameraCenterIsCockpit = cameraCenterIsCockpit;
                if (currentCameraCenterIsCockpit)
                    MoveCameraOutOfTrailSpace();
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
                if (currentlyControlled)
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

            positionCamera.positionBelowTarget = positionCameraBelowSub;

            if (currentlyControlled && !cameraCenterIsCockpit)
            {
                rotateCamera.enabled = true;

                if (freeCamera)
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
                            if (rotateCamera.IsTransitionDone)
                            {
                                ChangeState(CameraState.IsBound);

                                inWaterDirectionSource = new TransformDirectionSource(trailSpace);

                                if (nonCameraOrientation)
                                    nonCameraOrientation.isActive = false;
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
                    orientation.targetOrientation = outOfWater
                            ? fallOrientation
                            : inWaterDirectionSource;
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
                        if (!isMovingInReverse)
                        {
                            nonCameraOrientation.rightRotationSpeed = rightAxis * rotationDegreesPerSecond;
                            nonCameraOrientation.upRotationSpeed = -upAxis * rotationDegreesPerSecond;
                        }
                        else
                        {
                            nonCameraOrientation.rightRotationSpeed = -rightAxis * rotationDegreesPerSecond;
                            nonCameraOrientation.upRotationSpeed = upAxis * rotationDegreesPerSecond;
                        }
                    }
                }




                positionCamera.zoomAxis = zoomAxis;
            }
            else
            {
                if (nonCameraOrientation)
                {
                    nonCameraOrientation.isActive = false;
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
                    orientation.targetOrientation = fallOrientation;

            }

            if (orientation)
            {
                orientation.enabled = (!IsBoarded && (wasEverBoarded || !outOfWater)) && !batteryDead && !powerOff && !isAutoLeveling;
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
            if (currentlyControlled && !cameraCenterIsCockpit)
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


                    if (overdriveActive)
                    {
                        float overdriveThreshold = 0.5f;
                        if (forwardAxis > overdriveThreshold)
                        {
                            firstPersonMarkers.overdriveActive = true;
                            backFacingRight.overdrive =
                            backFacingLeft.overdrive =
                                (forwardAxis - overdriveThreshold) / (1f - overdriveThreshold);
                        }
                        else
                            backFacingLeft.overdrive = backFacingRight.overdrive = 0;
                    }
                    else
                        backFacingLeft.overdrive = backFacingRight.overdrive = 0;

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

            UpdateFirstPerson();

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
            
            Debug.LogException(ex);
        }
    }

    private void UpdateLighting()
    {
        try
        {
            hullLightController.lightsEnabled = lights;
        }
        catch (Exception ex)
        {
            Debug.LogError(nameof(UpdateLighting));
            Debug.LogException(ex);
        }
    }
    
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
            if (IsBoarded)
            {
                if (!rb.isKinematic)
                {
                    Log.LogWarning("Re-enabling kinematic state");
                    rb.SetKinematic();
                }
            }
            else if (rb.isKinematic)
            {
                Log.LogWarning("Re-disabling kinematic state");
                rb.UnsetKinematic();
            }

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
            if (forwardSpeed < -3)
                isMovingInReverse = true;
            else if (forwardSpeed > -2)
                isMovingInReverse = false;

            drag.enabled = !outOfWater;
        }
        catch (Exception ex)
        {
            Debug.LogError(nameof(MonitorPhysics));
            Debug.LogException(ex);
        }
    }

    public void Localize(Transform player)
    {
        player.parent = seat;
        player.localPosition = Vector3.zero;
        player.localEulerAngles = Vector3.zero;
    }

    public void UpdateLowCamera(float oceanY)
    {
        if (transform.position.y >= oceanY - 35 && transform.position.y < oceanY - 1)
            positionCameraBelowSub = true;
        else if (transform.position.y < oceanY - 40 || transform.position.y > oceanY - 2)
            positionCameraBelowSub = false;
    }
}


public enum UndockingCheckResult
{
    Ok,
    Busy,
    NotDocked,
    NotDockable,
}
