﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseRotateCamera : MonoBehaviour
{
    private ArchonControl dtc;
    // Start is called before the first frame update
    void Start()
    {
        dtc = GetComponent<ArchonControl>();
    }

    // Update is called once per frame
    void Update()
    {
        dtc.lookRightAxis = Input.GetAxis("Mouse X");
        dtc.lookUpAxis = Input.GetAxis("Mouse Y");

    }
}
