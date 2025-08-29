using Assets.Behavior.Adapters;
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
    public ArchonControl archon;
    private Color disabledColor = new Color(0.25f, 0.25f, 0.25f);
    public void OnTrigger()
    {
        using (var log = Log.New())
        {
            if (archon == null)
            {
                log.Error("ArButton: OnTrigger called without archon set.");
                return;
            }
            switch (function)
            {
                case Function.Undock:
                    archon.UndockSelected();
                    break;
                case Function.SelectLeft:
                    archon.SelectLeft();
                    break;
                case Function.SelectRight:
                    archon.SelectRight();
                    break;
                default:
                    break;
            }
        }
    }

    public bool IsEnabled
    {
        get
        {
            switch (function)
            {
                case Function.Undock:
                    return archon.HasSelectedDockable;
                case Function.SelectLeft:
                    return archon.bayControl.NumUndockableVehicles > 1;
                case Function.SelectRight:
                    return archon.bayControl.NumUndockableVehicles > 1;
                default:
                    return false;
            }
        }
    }

    public void OnHandOver()
    {
        handOverAge = 0;
    }
    private Color inactiveColor = Color.white;
    public Color activeColor = Color.white;
    private Renderer r;
    private Material material;
    public Function function = Function.None;
    // Start is called before the first frame update
    void Start()
    {
        if (archon == null)
            archon = GetComponentInParent<ArchonControl>();
        r = GetComponent<Renderer>();
        material = new Material(materialPrototype);
        r.material = material;
        inactiveColor = material.color;
    }
    public bool IsHandOver => handOverAge < 10;


    // Update is called once per frame
    void Update()
    {
        handOverAge++;

        material.color =
            IsEnabled
                ? (IsHandOver
                    ? activeColor
                    : inactiveColor
                    )
                : disabledColor;
    }
}
