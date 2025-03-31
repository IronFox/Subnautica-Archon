using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class FpsTest : MonoBehaviour
{
    private Vector3 preBoardingPosition;
    private LockedEuler preBoardingEuler;
    private Transform preBoardingParent;
    private bool isOnboarded;
    public ArchonControl subControl;
    private Rigidbody rb;

    public KeyCode boardKey = KeyCode.B;
    public KeyCode centerKey = KeyCode.C;
    public KeyCode outOfWaterKey = KeyCode.F;
    public KeyCode bayOpenKey = KeyCode.O;
    public KeyCode testUndock = KeyCode.U;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }


    void FixedUpdate()
    {
        var up = Input.GetAxis("Jump") - (Input.GetKey(KeyCode.LeftControl) ? 1 : 0);

        rb.AddRelativeForce(M.V3(Input.GetAxis("Horizontal"), up, Input.GetAxis("Vertical")) * 30);

    }

    // Update is called once per frame
    void Update()
    {
        if (!subControl.IsBeingControlled && Input.GetKeyDown(KeyCode.Mouse0))
        {
            var hits = Physics.RaycastAll(new Ray(transform.position, transform.forward), 2);


            foreach (var hit in hits)
            {
                var hatch = hit.collider.gameObject.GetComponent<DebugHatch>();
                if (hatch)
                {
                    if (!subControl.IsBoarded)
                        hatch.Board(rb, subControl);
                    else
                        hatch.Exit(rb, subControl);
                }
            }
        }
        if (!isOnboarded)
        {
            //Debug.Log(Input.GetAxis("Vertical"));


            LockedEuler
                .FromLocal(transform)
                .RotateBy(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"), Time.deltaTime * 800)
                .ApplyTo(transform);
        }


        if (Input.GetKeyDown(testUndock))
        {
            if (subControl.hangarRoot.childCount == 0)
            {
                Debug.LogError($"Not docked sub to undock");
            }
            else
            {
                var v = subControl.hangarRoot.GetChild(0);
                subControl.Undock(v.gameObject);
            }
        }

        if (Input.GetKeyDown(outOfWaterKey))
        {
            subControl.outOfWater = !subControl.outOfWater;
        }

        if (Input.GetKeyDown(centerKey))
        {
            subControl.cameraCenterIsCockpit = !subControl.cameraCenterIsCockpit;
        }

        if (Input.GetKeyDown(bayOpenKey))
        {
            subControl.openBay = !subControl.openBay;
        }

        if (Input.GetKeyDown(boardKey))
        {
            ConsoleControl.Write(boardKey.ToString());
            if (!isOnboarded)
            {
                ConsoleControl.Write("Boarding");
                try
                {
                    preBoardingPosition = transform.position;
                    preBoardingEuler = LockedEuler.FromGlobal(transform);
                    preBoardingParent = transform.parent;
                    subControl.Localize(transform);
                    subControl.Control(gameObject);
                }
                catch (Exception ex)
                {
                    ConsoleControl.WriteException("Onboarding failed", ex);
                }
                isOnboarded = true;
                ConsoleControl.Write("Boarded");
            }
            else
            {
                ConsoleControl.Write("Offboarding");
                subControl.ExitControl(gameObject);
                transform.parent = preBoardingParent;
                transform.position = preBoardingPosition;
                preBoardingEuler.ApplyTo(transform);
                isOnboarded = false;
                ConsoleControl.Write("Offboarded");
            }

        }
    }
}
