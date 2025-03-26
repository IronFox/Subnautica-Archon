using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RudderControl : MonoBehaviour
{
    private Vector3 lastPosition;
    // Start is called before the first frame update
    void Start()
    {
        lastPosition = transform.parent.position;
    }

    private const float maxDegPerSecond = 60;

    // Update is called once per frame
    void Update()
    {
        var t = transform.parent;
        var newPosition = t.position;
        if (Time.deltaTime > 0)
        {
            var velocity = (newPosition - lastPosition) / Time.deltaTime;
            var forward = transform.forward * M.Dot(velocity, transform.forward);
            var projected = velocity - forward;
            velocity = forward + velocity * 2 + t.forward;
            
            //var projected = velocity - transform.up * M.Dot(velocity, transform.up);
            var local = t.InverseTransformDirection(velocity);
            var angle = -M.RadToDeg(Mathf.Atan2(local.z, local.x)) + 90;
            angle = Mathf.Repeat(angle+180, 360) - 180;
            angle = Mathf.Clamp(angle , -45,45);

            var current = transform.localEulerAngles.y;
            var delta = Mathf.Repeat( angle - current + 180, 360) - 180;

            var maxRotNow = maxDegPerSecond * Time.deltaTime;
            if (Mathf.Abs(delta) > maxRotNow)
                angle = current + maxRotNow * Mathf.Sign(delta);
            transform.localEulerAngles = new Vector3(0, angle, 0);
        }

        lastPosition = newPosition;
    }
}
