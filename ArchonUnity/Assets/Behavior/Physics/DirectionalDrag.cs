using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DirectionalDrag : MonoBehaviour
{
    public Vector3 remainingVelocityAfterOneSecond = M.V3(0.3f, 0.3f, 0.7f);
    private Rigidbody rb;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        var local = transform.InverseTransformDirection(rb.velocity);
        var remainingLocalVelocity = M.Scale(local,M.Pow(remainingVelocityAfterOneSecond, Time.fixedDeltaTime));
        var change = remainingLocalVelocity - local;
        rb.AddRelativeForce(change,ForceMode.VelocityChange);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
