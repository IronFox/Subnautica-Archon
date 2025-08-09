using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeatEngine : MonoBehaviour
{
	public SeatNodeController node0;
	public SeatNodeController node1;
	public SeatNodeController node2;
	public HelmSeatController seatRoot;
	// Start is called before the first frame update
	void Start()
    {
        
    }

	private bool wasParked;

	// Update is called once per frame
	void Update()
	{
		if (seatRoot.IsParked && wasParked)
			return;
		var q = seatRoot.transform.rotation;
		var local = Quaternion.Inverse(transform.rotation) * q;
		var euler = local.eulerAngles;
		node0.RotateTo(euler.x, seatRoot.transform);
		node1.RotateTo(euler.y, seatRoot.transform);
		node2.RotateTo(euler.z, seatRoot.transform);
		wasParked = seatRoot.IsParked;
	}
}
