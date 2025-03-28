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
    public bool isControlled;

    //public bool positionCameraBelowSub;

    public bool cameraCenterIsCockpit;
    public bool powerOff;
    public bool batteryDead;
    public bool openUpgradeCover;
    public bool openBay;

    private DateTime lastOnboarded;

    private GameObject isEnteredBy;
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

    private DirectAt look;
    private RudderControl[] rudders;
    private Rigidbody rb;
    private bool currentlyControlled;

    private Parentage onboardLocalizedTransform;
    private Parentage cameraMove;

    private bool currentCameraCenterIsCockpit;
    private bool cameraIsInTrailspace;

    private bool wasEverBoarded;

    public LogConfig log = LogConfig.Default;
    private enum CameraState
    {
        IsFree,
        IsBound,
        IsTransitioningToBound
    }

    private CameraState state = CameraState.IsBound;

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


    public void Enter(GameObject playerRoot)
    {
        rb.isKinematic = true;
        isEnteredBy = playerRoot;
        var colliders = playerRoot.GetComponentsInChildren<Collider>();
        foreach (var collider in colliders)
        {
            Physics.IgnoreLayerCollision(collider.gameObject.layer, OuterShellLayer, true);
        }

        if (interior)
        {
            foreach (var r in interior.GetComponentsInChildren<Renderer>())
                r.enabled = true;
            foreach (var r in interior.GetComponentsInChildren<Collider>())
                r.enabled = true;
        }

    }

    public void Exit()
    {

        if (interior)
        {
            foreach (var r in interior.GetComponentsInChildren<Renderer>())
                r.enabled = false;
            foreach (var r in interior.GetComponentsInChildren<Collider>())
                r.enabled = false;
        }

        if (isEnteredBy)
        {
            var colliders = isEnteredBy.GetComponentsInChildren<Collider>();
            foreach (var collider in colliders)
            {
                Physics.IgnoreLayerCollision(collider.gameObject.layer, OuterShellLayer,false);
            }
            isEnteredBy = null;
            rb.isKinematic = false;
        }
    }


    public void Control(Transform localizeInsteadOfMainCamera = null)
    {
        wasEverBoarded = true;
        lastOnboarded = DateTime.Now;
        if (!currentlyControlled)
        {
            log.Write($"Controlling");

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

            currentlyControlled = isControlled = true;

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
                currentlyControlled = isControlled = false;
                log.Write($"Reintegration trail space");
                trailSpace.parent = transform;
            }

            listeners.SignalOffBoardingEnd();

        }
    }

    // Start is called before the first frame update
    void Start()
    {
        nonCameraOrientation = GetComponent<NonCameraOrientation>();
        rb = GetComponent<Rigidbody>();
        look = GetComponent<DirectAt>();
        rudders = GetComponentsInChildren<RudderControl>();
        rotateCamera = trailSpace.GetComponent<RotateCamera>();
        positionCamera = trailSpace.GetComponent<PositionCamera>();
        fallOrientation = GetComponent<FallOrientation>();
        energyLevel = GetComponentInChildren<EnergyLevel>();
        firstPersonMarkers = GetComponentInChildren<FirstPersonMarkers>();
        bayControl = GetComponent<BayControl>();
        if (look != null)
            look.targetOrientation = inWaterDirectionSource = new TransformDirectionSource(trailSpace);

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

    void Update()
    {
        try
        {
            firstPersonMarkers.overdriveActive = false;

            ProcessUpgradeCover();

            look.rotateZ = !outOfWater;
            var projection = look.Intention.TranslateBy(rb.velocity);
            foreach (var rudder in rudders)
                rudder.UpdateIntention(projection);


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

            bayControl.open = openBay;

            firstPersonMarkers.show = 
                positionCamera.isFirstPerson
                && isControlled
                && !batteryDead
                && !powerOff;

            foreach (var h in healingLights)
                h.isHealing = isHealing;

            energyHistory.Add(currentEnergy);
            var edge = energyHistory.GetEdge();
            if (energyLevel != null)
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

            if (currentlyControlled != isControlled)
            {
                if (!isControlled)
                    ExitControl();
                else
                    Control();
            }

            if (currentCameraCenterIsCockpit != cameraCenterIsCockpit && currentlyControlled)
            {
                currentCameraCenterIsCockpit = cameraCenterIsCockpit;
                if (currentCameraCenterIsCockpit)
                    MoveCameraOutOfTrailSpace();
                else
                    MoveCameraToTrailSpace();
            }

            if (Input.GetKeyDown(openConsoleKey))
            {
                if (currentlyControlled)
                {
                    statusConsole.ToggleVisibility();

                    //ConsoleControl.Write("Capturing debug information v3");

                    //ConsoleControl.Write($"3rd person camera at {trailSpace.position}");
                    //ConsoleControl.Write($"Main camera at {cameraRoot.position}");
                    ////ConsoleControl.Write($"Cockpit center at {cockpitRoot.position}");


                    ////ConsoleControl.Write($"RigidBody.isKinematic="+rb.isKinematic);
                    ////ConsoleControl.Write($"RigidBody.constraints="+rb.constraints);
                    ////ConsoleControl.Write($"RigidBody.collisionDetectionMode=" +rb.collisionDetectionMode);
                    ////ConsoleControl.Write($"RigidBody.drag=" +rb.drag);
                    ////ConsoleControl.Write($"RigidBody.mass=" +rb.mass);
                    ////ConsoleControl.Write($"RigidBody.useGravity=" +rb.useGravity);
                    ////ConsoleControl.Write($"RigidBody.velocity=" +rb.velocity);
                    ////ConsoleControl.Write($"RigidBody.worldCenterOfMass=" +rb.worldCenterOfMass);

                    //LogComposition(transform);

                }
                else
                    log.Write($"Not currently boarded. Ignoring console key");

            }


            rotateCamera.rotationAxisX = lookRightAxis;
            rotateCamera.rotationAxisY = lookUpAxis;

            positionCamera.positionBelowTarget = false/*positionCameraBelowSub*/;

            if (currentlyControlled && !cameraCenterIsCockpit)
            {
                rotateCamera.enabled = true;

                if (freeCamera)
                {
                    rotateCamera.AbortTransition();
                    ChangeState(CameraState.IsFree);
                    inWaterDirectionSource = nonCameraOrientation;
                    if (nonCameraOrientation != null)
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

                                if (nonCameraOrientation != null)
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

                if (look != null)
                    look.targetOrientation = outOfWater
                            ? fallOrientation
                            : inWaterDirectionSource;
                nonCameraOrientation.outOfWater = outOfWater;

                if (outOfWater)
                {
                    if (nonCameraOrientation != null)
                    {
                        nonCameraOrientation.rightRotationSpeed = 0;
                        nonCameraOrientation.upRotationSpeed = 0;
                    }
                    backFacingLeft.thrust = 0;
                    backFacingRight.thrust = 0;

                    backFacingLeft.overdrive = 0;
                    backFacingRight.overdrive = 0;
                }
                else
                {
                    if (nonCameraOrientation != null)
                    {
                        nonCameraOrientation.rightRotationSpeed = rightAxis * rotationDegreesPerSecond;
                        nonCameraOrientation.upRotationSpeed = -upAxis * rotationDegreesPerSecond;
                    }
                    backFacingLeft.thrust = forwardAxis + look.HorizontalRotationIntent * 0.001f;
                    backFacingRight.thrust = forwardAxis - look.HorizontalRotationIntent * 0.001f;


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




                positionCamera.zoomAxis = zoomAxis;
            }
            else
            {
                if (nonCameraOrientation != null)
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
                backFacingLeft.thrust = 0;
                backFacingRight.thrust = 0;
                if (look != null)
                    look.targetOrientation = fallOrientation;

            }

            if (look != null)
                look.enabled = (isControlled || (outOfWater && wasEverBoarded)) && !batteryDead && !powerOff;

            forwardFacingLeft.thrust = -backFacingLeft.thrust;
            forwardFacingRight.thrust = -backFacingRight.thrust;

            //rb.drag = outOfWater ? airDrag : waterDrag;

            //rb.useGravity = outOfWater && !isDocked;
        }
        catch (Exception ex)
        {
            ConsoleControl.WriteException($"EchelongControl.Update()", ex);
        }
    }

    public void Localize(Transform player)
    {
        player.parent = seat;
        player.localPosition = Vector3.zero;
        player.localEulerAngles = Vector3.zero;
    }

}