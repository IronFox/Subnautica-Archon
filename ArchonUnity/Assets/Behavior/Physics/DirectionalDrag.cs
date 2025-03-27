using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DirectionalDrag : MonoBehaviour
{
    //public Vector3 remainingVelocityAfterOneSecond = M.V3(0.3f, 0.3f, 0.7f);
    //public Vector3 
    public BoxCollider entireBoundingBox;
    private Rigidbody rb;
    public float density = 0.1f;
    private Vector3 dragCoefficient = M.V3(0.7f, 0.7f, 0.1f);
    private Vector3 linearDrag = M.V3(3f, 3f, 0.5f)*10000f;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        //D = Cd * A * .5 * r * V^2
        var surfaceArea = M.Mult(entireBoundingBox.size, entireBoundingBox.size)*0.3f;
        surfaceArea = M.V3(M.Max(surfaceArea.x, surfaceArea.y), M.Max(surfaceArea.x, surfaceArea.y), surfaceArea.z);    //x=y, less weirdness
        var localVelocity = transform.InverseTransformDirection(rb.velocity);
        var quadtraticLocalDragForce = M.Mult(localVelocity, localVelocity, surfaceArea, dragCoefficient) * density * 0.5f;
        var linearLocalDragForce = M.Mult(localVelocity,linearDrag)* density;
        var newVelocity = localVelocity.Combine(linearLocalDragForce, quadtraticLocalDragForce , (v,l, q) =>
        {
            bool neg = v < 0;
            if (neg)
            {
                v = -v;
                l = -l;
            }


            float a = (l+q) / rb.mass * Time.fixedDeltaTime;
            if (v > a)
                v -= a;
            else
                v = 0;
            if (neg)
                v = -v;
            return v;
        });
        rb.velocity = transform.TransformDirection(newVelocity);
        


        //rb.AddRelativeForce(-dragForce, ForceMode.Force);
        //Debug.Log(linearLocalDragForce);




        //var remainingLocalVelocity = M.Mult(local,M.Pow(remainingVelocityAfterOneSecond, Time.fixedDeltaTime));

        //var change = remainingLocalVelocity - local;
        //rb.AddRelativeForce(change,ForceMode.VelocityChange);
    }

    // Update is called once per frame
    void Update()
    {
     
    }
}
