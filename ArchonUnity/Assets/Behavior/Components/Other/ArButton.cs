using UnityEngine;

public class ArButton : MonoBehaviour
{
    public enum Function
    {
        None,
        Undock,
        SelectLeft,
        SelectRight,
    }
    private int handOverAge = 0;

    public Material materialPrototype;

    public void OnTrigger()
    {

    }

    public void OnHandOver()
    {
        handOverAge = 0;
    }


    public Function function = Function.None;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        handOverAge++;
    }
}
