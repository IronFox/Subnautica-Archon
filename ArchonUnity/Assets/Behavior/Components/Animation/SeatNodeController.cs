using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeatNodeController : MonoBehaviour
{

	internal void RotateTo(float angle, Transform seatRoot)
	{
		transform.localEulerAngles = new Vector3(0,angle, 0);
		foreach (Transform t in transform)
			t.rotation = Quaternion.LookRotation(seatRoot.position - t.position, transform.parent.up);
	}

	// Start is called before the first frame update
	void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
