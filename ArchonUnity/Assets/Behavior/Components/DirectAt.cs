using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using static UnityEngine.GraphicsBuffer;

public class DirectAt : MonoBehaviour
{
    public IDirectionSource targetOrientation;

    private Rigidbody rb;

    float rotX = 0;

    public bool isMovingInReverse;
    public bool rotateUpDown = true;
    public bool rotateZ = true;
    private const float rotationSpeed = 30f;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    public ProjectedMotionSpace Intention { get; private set; }


    private Vector2 Flat(Vector3 source) => M.FlatNormalized(source);

    public float HorizontalRotationIntent => enabled ? rotX : 0;

    private float SignedMin(float signed, float max)
    {
        return Mathf.Sign(signed) * Mathf.Min(Mathf.Abs(signed), max);
    }


    private float UpAngle(Vector3 vector, Vector2 flatAxis)
    {
        var forward = M.FlatNormal(flatAxis);
        return Vector3.SignedAngle(M.UnFlat(forward), vector, M.UnFlat(flatAxis));
        //=> Vector3.ang
        //return Mathf.Atan2(vector.y, Vector2.Dot(Flat(vector),forward)) * 180f / Mathf.PI;
    }
    
    // Update is called once per frame
    void FixedUpdate()
    {
        Intention = new ProjectedMotionSpace(transform.position);
        rotX = RotateHorizontal();
        //RotateDirect();
        if (rotateZ)
            RotateZ(rb, Mathf.Clamp( -rotX*3,-45,45), targetOrientation.ZImpact);
        if (rotateUpDown)
            RotateUpDown();

        //rb.AddRelativeForce(0, 0, 10, ForceMode.Acceleration);
    }

    //void LateUpdate()
    //{
    //    rb.transform.eulerAngles = new Vector3(rb.transform.eulerAngles.x, rb.transform.eulerAngles.y, -rotX / 10);

    //}

    public void RotateZ(Rigidbody rb, float targetZ, float targetImpact)
    {
        if (isMovingInReverse)
            targetZ = -targetZ;
        var axis = rb.transform.forward;
        //var correct = -Vector3.Dot(rb.angularVelocity, axis);
        //rb.AddTorque(axis * correct, ForceMode.VelocityChange);



        var delta = targetZ - rb.rotation.eulerAngles.z;
        while (delta < -180)
            delta += 360;
        while (delta > 180)
            delta -= 360;

        var (accel, _) = Adjust(delta, M.RadToDeg(M.Dot(rb.angularVelocity, axis)),axis, isZ: true);

        //float wantTurn = -delta * 1.5f;
        //if (Mathf.Abs(delta) < 0.1f)
        //    wantTurn = 0;
        ////SignedMin(delta * horizontalRotationAcceleration, maxHorizontalRotationSpeed);
        //float haveTurn = -Vector3.Dot(rb.angularVelocity, axis) * 180 / Mathf.PI;
        //float error = (wantTurn - haveTurn) * targetImpact;
        //float accel = error * 10 * 0.02f;

        //SignedMin((wantTurn - haveTurn)*10, 10);
        //rb.AddTorque(axis * -accel, ForceMode.Acceleration);

    }

    private (float Acceleration, float AngleError) Adjust(float angleError, float haveTurn, Vector3 axis, bool isZ = false)
    {
        float wantTurn = M.SignedMin(angleError/10, 1) * rotationSpeed;
        if (isZ)
            wantTurn *= 1.5f;
        if (Mathf.Abs(wantTurn) < 1f)
        {
            wantTurn = 0;
            angleError = 0;
        }
        float error = (wantTurn - haveTurn) * targetOrientation.Impact;


        float accel = M.SignedMin( error * 0.1f, 10f);
        if (isZ)
            accel *= 1.5f;

        Intention.RotateThisBy(axis, wantTurn);

        rb.AddTorque(axis * accel, ForceMode.Acceleration);


        return (accel, angleError);
    }

    private void RotateUpDown()
    {
        var axis = -M.UnFlat(M.FlatNormal(Flat(rb.transform.forward)));
//            Unflat(Flat(rb.transform.right));
            //Vector3.Cross(Vector3.up, rb.transform.forward);
        float have = UpAngle(rb.transform.forward, Flat(axis));
        float want = UpAngle(targetOrientation.Forward, Flat(targetOrientation.Right));

        var delta = Mathf.DeltaAngle(have, want);
        var (accel, _) = Adjust(delta, M.RadToDeg( M.Dot(rb.angularVelocity, axis)),axis);


    }





    private float RotateHorizontal()
    {
        var upDownAxis = rb.transform.right;

        var normalForward = M.FlatNormal(Flat(upDownAxis));
        var directForward = Flat(rb.transform.forward);
        var correctedForward = (directForward + normalForward) / 2;
        var forward = Mathf.Atan2(correctedForward.y, correctedForward.x) * 180f / Mathf.PI;
        var error = Vector2.SignedAngle(Flat(targetOrientation.Forward), directForward);


        float currentZAngle = rb.rotation.eulerAngles.z;
        while (currentZAngle < -180)
            currentZAngle += 360;
        while (currentZAngle > 180)
            currentZAngle -= 360;
        //+45 .. -45
        if (isMovingInReverse)
            currentZAngle = -currentZAngle;
        float zError = Mathf.Abs(Mathf.Clamp(currentZAngle / 45, -1, 1) + Mathf.Clamp(error / 10, -1, 1));
        float zModifier = (2f - zError) * 0.5f;
        float accel;
        float have = /*rb.angularVelocity.y*/M.RadToDeg(M.Dot(rb.angularVelocity, transform.up));
        (accel, error) = Adjust(error * zModifier, have, Vector3.up);
        return error;

    }
}
