using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadStripeColor : MonoBehaviour, IColorListener
{
    public int materialIndex;
    private MeshRenderer myRenderer;

    public void SetColors(Color mainColor, Color stripeColor)
    {
        if (materialIndex < myRenderer.materials.Length)
            myRenderer.materials[materialIndex].color = stripeColor;
    }

    // Start is called before the first frame update
    void Start()
    {
        myRenderer = GetComponent<MeshRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
