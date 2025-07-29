using UnityEngine;


public class FpsTest : MonoBehaviour
{
    private bool IsAtHelm => atHelm != null;
    private bool isOnBoard;
    public ArchonControl subControl;
    public Transform head;
    public Transform body;
    private Rigidbody rb;

    public KeyCode controlKey = KeyCode.B;
    public KeyCode centerKey = KeyCode.LeftControl;
    public KeyCode outOfWaterKey = KeyCode.F;
    public KeyCode bayOpenKey = KeyCode.O;
    public KeyCode testUndock = KeyCode.U;
    private DebugHelm atHelm;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        PlayerAdapter.PlayerReference = gameObject;
        OnExit(transform.position);
    }

    void FixedUpdate()
    {
        if (!IsAtHelm)
        {
            var up = Input.GetAxis("Jump") - (Input.GetKey(KeyCode.C) ? 1 : 0);
            var relativeX = transform.right * Input.GetAxis("Horizontal");
            var relativeZ = transform.forward * Input.GetAxis("Vertical");
            var relativeY = transform.up * up;

            rb.AddForce((relativeX + relativeY + relativeZ) * 30);
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (!subControl.IsBeingControlled)
        {
            var hits = Physics.RaycastAll(new Ray(head.position, head.forward), 2);

            var click = Input.GetKeyDown(KeyCode.Mouse0);

            foreach (var hit in hits)
            {
                var target = hit.collider.gameObject.GetComponent<DebugHandTarget>();
                if (target)
                    if (click)
                        target.OnTrigger(subControl, this);
                    else
                        target.OnHandOver(subControl, this);
            }
        }
        if (!IsAtHelm)
        {
            //Debug.Log(Input.GetAxis("Vertical"));

            if (isOnBoard)
            {
                LockedEuler
                    .FromLocal(transform)
                    .RotateBy(0, Input.GetAxis("Mouse X"), Time.deltaTime * 800)
                    .ApplyTo(transform);
                LockedEuler
                    .FromLocal(head)
                    .RotateBy(-Input.GetAxis("Mouse Y"), 0, Time.deltaTime * 800)
                    .Constrained(angle => Mathf.Clamp(angle, -70, 70), null)
                    .ApplyTo(head);
            }
            else
                LockedEuler
                    .FromLocal(transform)
                    .RotateBy(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"), Time.deltaTime * 800)
                    .ApplyTo(transform);
        }
        else
        {
            if (subControl.ShouldBeAbleToTurnHead)
                LockedEuler
                    .FromLocal(head)
                    .RotateBy(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"), Time.deltaTime * 800)
                    .ConstrainedHead()
                    .ApplyTo(head);
        }


        if (Input.GetKeyDown(testUndock))
        {
            if (subControl.hangarRoot.childCount == 0)
            {
                Debug.LogError($"Not docked sub to undock");
            }
            else
            {
                var v = subControl.hangarRoot.GetChild(0);
                subControl.Undock(v.gameObject);
            }
        }

        if (Input.GetKeyDown(outOfWaterKey))
        {
            subControl.outOfWater = !subControl.outOfWater;
        }

        //if (Input.GetKeyDown(centerKey))
        //{
        //    subControl.cameraCenterIsCockpit = !subControl.cameraCenterIsCockpit;
        //}


        if (Input.GetKeyDown(controlKey) && IsAtHelm)
        {
            ConsoleControl.Write(controlKey.ToString());
            ConsoleControl.Write("Offboarding");
            if (subControl.ExitControl(ToReference(), false))
            {
                transform.parent = null;
                transform.position = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                transform.SetParent(null);
                transform.position = atHelm.exit.position;
                atHelm = null;
                ConsoleControl.Write("Offboarded");
            }
            else
                ConsoleControl.Write("Offboarding failed");
        }
    }

    internal void OnBoard(Vector3 entryPosition)
    {
        rb.useGravity = true;
        rb.transform.position = entryPosition;
        isOnBoard = true;

        body.localEulerAngles = new Vector3(0, 0, 0);
        body.localPosition = new Vector3(0, 0.7f, 0);
        head.localPosition = new Vector3(0, 1.6f, 0);
        transform.up = Vector3.up;
    }

    internal void OnExit(Vector3 exitPosition)
    {
        rb.transform.position = exitPosition;
        rb.useGravity = false;
        isOnBoard = false;

        body.localEulerAngles = new Vector3(90, 0, 0);
        body.localPosition = new Vector3(0, 0, 0.7f);
        head.localPosition = new Vector3(0, 0, 1.6f);
    }

    internal PlayerReference ToReference()
    {
        return new PlayerReference(gameObject, head);
    }

    internal void EnterHelm(ArchonControl archon, DebugHelm debugHelm)
    {
        atHelm = debugHelm;
        transform.SetParent(debugHelm.transform);
        transform.localPosition = M.V3(0, -1.6f, 0);
        transform.localRotation = Quaternion.identity;

        archon.Control(ToReference());

    }
}
