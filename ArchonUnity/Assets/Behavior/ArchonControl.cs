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

    public float forwardAxis;
    public float rightAxis;
    public float upAxis;
    public float zoomAxis;
    public float lookRightAxis;
    public float lookUpAxis;

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

    private DateTime lastOnboarded;

    private GameObject isEnteredBy;
    private GameObject controlledFromEntered;
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

    private DirectAt orientation;
    private RudderControl[] rudders;
    private Rigidbody rb;
    private bool currentlyControlled;

    private Parentage onboardLocalizedTransform;
    private Parentage cameraMove;

    private bool currentCameraCenterIsCockpit;
    private bool cameraIsInTrailspace;

    private bool wasEverBoarded;

    public bool IsBeingControlled => currentlyControlled;

    public LogConfig log = LogConfig.Default;
    private enum CameraState
    {
        IsFree,
        IsBound,
        IsTransitioningToBound
    }

    private CameraState state = CameraState.IsBound;


    public bool IsBoarded => isEnteredBy;

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
            log.Write("Moving camera to trailspace. Setting secondary fallback camera transform");
            
            CameraUtil.secondaryFallbackCameraTransform = trailSpaceCameraContainer;

            cameraMove = Parentage.FromLocal(cameraRoot);
            cameraRoot.parent = trailSpaceCameraContainer;
            TransformDescriptor.LocalIdentity.ApplyTo(cameraRoot);
            log.Write("Moved");
        }
    }

    private void MoveCameraOutOfTrailSpace()
    {
        if (cameraIsInTrailspace)
        {
            cameraIsInTrailspace = false;

            log.Write("Moving camera out of trailspace. Unsetting secondary fallback camera transform");
            
            CameraUtil.secondaryFallbackCameraTransform = null;

            cameraMove.Restore();
            log.Write("Moved");
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

    public void Enter(GameObject playerRoot)
    {
        rb.isKinematic = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rb.interpolation = RigidbodyInterpolation.None;


        isEnteredBy = playerRoot;
        var colliders = playerRoot.GetComponentsInChildren<Collider>();
        foreach (var collider in colliders)
        {
            Physics.IgnoreLayerCollision(collider.gameObject.layer, OuterShellLayer, true);
        }

        SetRenderAndCollisionActive(interior, true);
        SetRenderAndCollisionActive(exterior, false);
    }

    public void Exit()
    {

        SetRenderAndCollisionActive(interior, false);
        SetRenderAndCollisionActive(exterior, true);

        if (isEnteredBy)
        {
            var colliders = isEnteredBy.GetComponentsInChildren<Collider>();
            foreach (var collider in colliders)
            {
                Physics.IgnoreLayerCollision(collider.gameObject.layer, OuterShellLayer,false);
            }
            isEnteredBy = null;
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Extrapolate;
        }
    }


    public void Control(Transform localizeInsteadOfMainCamera = null)
    {
        wasEverBoarded = true;
        lastOnboarded = DateTime.Now;
        if (!currentlyControlled)
        {
            log.Write($"Controlling");

            controlledFromEntered = isEnteredBy;
            Exit();

            var listeners = BoardingListeners.Of(this, trailSpace);

            listeners.SignalOnboardingBegin();

            cameraRoot = localizeInsteadOfMainCamera;
            if (cameraRoot == null)
                cameraRoot = Camera.main.transform;
            log.Write($"Setting {cameraRoot} as cameraRoot");
            CameraUtil.primaryFallbackCameraTransform = cameraRoot;
            onboardLocalizedTransform = Parentage.FromLocal(cameraRoot);

            cameraIsInTrailspace = false;//just in case
            if (!currentCameraCenterIsCockpit)
                MoveCameraToTrailSpace();

            log.Write($"Offloading trail space");
            trailSpace.parent = transform.parent;

            currentlyControlled = true;

            listeners.SignalOnboardingEnd();
        }
    }



    public void ExitControl()
    {
        if (currentlyControlled)
        {
            log.Write($"Exiting control");
            var listeners = BoardingListeners.Of(this, trailSpace);
            try
            {

                listeners.SignalOffBoardingBegin();

                MoveCameraOutOfTrailSpace();
                log.Write($"Restoring parentage");
                onboardLocalizedTransform.Restore();
            }
            finally
            {
                currentlyControlled = false;
                log.Write($"Reintegration trail space");
                trailSpace.parent = transform;
            }

            if (controlledFromEntered)
            {
                //if (controlExit)
                //{
                //    controlledFromEntered.transform.position = controlExit.position;
                //}
                Enter(controlledFromEntered);
                controlledFromEntered = null;
            }
            listeners.SignalOffBoardingEnd();

        }
    }

    // Start is called before the first frame update
    void Start()
    {
        nonCameraOrientation = GetComponent<NonCameraOrientation>();
        rb = GetComponent<Rigidbody>();
        orientation = GetComponent<DirectAt>();
        rudders = GetComponentsInChildren<RudderControl>();
        rotateCamera = trailSpace.GetComponent<RotateCamera>();
        positionCamera = trailSpace.GetComponent<PositionCamera>();
        fallOrientation = GetComponent<FallOrientation>();
        energyLevel = GetComponentInChildren<EnergyLevel>();
        firstPersonMarkers = GetComponentInChildren<FirstPersonMarkers>();
        bayControl = GetComponent<BayControl>();
        if (orientation)
            orientation.targetOrientation = inWaterDirectionSource = new TransformDirectionSource(trailSpace);

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
        ExitControl();
        
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
            statusConsole.Set(StatusProperty.IsControlled, currentlyControlled);
            statusConsole.Set(StatusProperty.IsEntered, !!isEnteredBy);
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
                var projection = orientation.Intention.TranslateBy(rb.velocity);
                foreach (var rudder in rudders)
                    rudder.UpdateIntention(projection);
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
            bayControl.open = openBay;
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
                    log.Write($"Not currently boarded. Ignoring console key");

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
                        nonCameraOrientation.rightRotationSpeed = rightAxis * rotationDegreesPerSecond;
                        nonCameraOrientation.upRotationSpeed = -upAxis * rotationDegreesPerSecond;
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
                orientation.enabled = (currentlyControlled || (outOfWater && wasEverBoarded)) && !batteryDead && !powerOff && !isAutoLeveling;

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
        }
        catch (Exception ex)
        {
            
            Debug.LogException(ex);
        }
    }

    private void MonitorPhysics()
    {
        try
        {
            if (isEnteredBy)
            {
                if (!rb.isKinematic)
                {
                    log.LogWarning("Re-enabling kinematic state");
                    rb.isKinematic = true;

                }
            }
            if (rb.drag != 0)
            {
                log.LogWarning("Re-setting drag to 0");
                rb.drag = 0;
            }
            if (rb.angularDrag != 1)
            {
                log.LogWarning("Re-setting angular drag to 1");
                rb.angularDrag = 1;
            }
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