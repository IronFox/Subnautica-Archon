using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MotionSimulation : MonoBehaviour
{
    private ArchonControl control;
    private Rigidbody rb;
    private DirectionalDrag drag;
    public Transform oceanSurface;
    // Start is called before the first frame update
    void Start()
    {
        control = GetComponent<ArchonControl>();
        rb = GetComponent<Rigidbody>();
        drag = GetComponent<DirectionalDrag>();
    }

    // Update is called once per frame
    void Update()
    {
        control.forwardAxis = Input.GetAxis("Vertical");
        control.rightAxis = Input.GetAxis("Horizontal");
        control.upAxis = Input.GetAxis("Jump") - (Input.GetKey(KeyCode.C) ? 1 : 0);
        control.overdriveActive = Input.GetKey(KeyCode.LeftShift);
        control.freeCamera = Input.GetMouseButton(1);
        control.zoomAxis = -Input.GetAxis("Mouse ScrollWheel");
        
    }

    void FixedUpdate()
    {
        if (oceanSurface != null)
        {
            control.outOfWater = control.transform.position.y >= oceanSurface.position.y;

            control.UpdateLowCamera(oceanSurface.position.y);
        }
        if (control.IsBeingControlled && !control.outOfWater && !control.doAutoLevel)
        {
            if (rb == null)
            {
                return;
            }
            var forwardAccel = control.forwardAxis * 50;
            var hAccel = control.freeCamera ? 0:
                            control.rightAxis * 20;
            var upAccel = control.freeCamera ? 0:
                            control.upAxis * 20;

            try
            {
                rb.AddRelativeForce(hAccel, upAccel, forwardAccel, ForceMode.Acceleration);
            }
            catch (Exception ex)
            {
                ConsoleControl.WriteException("FixedUpdate()", ex);
            }


        }
        if (control.outOfWater)
        {
            rb.useGravity = true;
            drag.density = 0.01f;
        }
        else
        {
            rb.useGravity = false;
            drag.density = 0.5f;
        }
    }
}
