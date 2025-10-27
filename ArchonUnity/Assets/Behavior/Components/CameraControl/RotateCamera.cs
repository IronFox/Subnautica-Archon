using UnityEngine;

public class RotateCamera : MonoBehaviour
{
    public float rotationAxisX;
    public float rotationAxisY;
    public float maxDegreesPerSecond = 800;
    public float transitionSpeedMultiplier = 2;
    public MapControl hudMap;

    public LockedEuler Current => current;
    private LockedEuler current;
    private Transform transitionTarget;
    private bool transitioning;
    private float transitionProgress;
    public ArchonControl archon;

    // Start is called before the first frame update
    void Start()
    {
        current = LockedEuler.FromGlobal(transform);
    }

    // Update is called once per frame
    void Update()
    {
        current = current.RotateBy(-rotationAxisY, rotationAxisX, maxDegreesPerSecond * Time.deltaTime);
        if (!transitioning)
        {
            current.ApplyTo(transform);
        }
        else
        {
            transitionProgress += Time.deltaTime * transitionSpeedMultiplier;
            transitionProgress = Mathf.Clamp01(transitionProgress);
            //Debug.Log($"@{transitionProgress}");

            var interpolated = LockedEuler.Slerp(current, LockedEuler.FromGlobal(transitionTarget), transitionProgress);

            interpolated.ApplyTo(transform);
        }
        if (hudMap)
        {
            hudMap.transform.localRotation =
                Quaternion.Euler(0, 0, 40) *
                Quaternion.Euler(-50, 0, 0) *
                Quaternion.Euler(0, -transform.rotation.eulerAngles.y, 0)
                ;

            //    rotation = Quaternion.identity;

            hudMap.displayShip.transform.localRotation = archon.transform.rotation;

            //hudMap.display.transform.rotation = Quaternion.LookRotation(new Vector3(transform.forward.x, 0, transform.forward.z), Vector3.up);
            //hudMap.upClip = 1f;
            //hudMap.downClip = -1f;

            //float delta = transform.rotation.eulerAngles.x - 343f;
            //if (delta > 180f)
            //    delta -= 360f;
            //if (delta < -180f)
            //    delta += 360f;

            //if (Mathf.Abs(delta) < 20f)
            //{
            //    hudMap.upClip = 0.25f;
            //    hudMap.downClip = -0.25f;
            //}
            //else if (delta > 0)
            //{
            //    hudMap.upClip = 0.01f;
            //    hudMap.downClip = -0.25f;
            //}
            //else
            //{
            //    hudMap.upClip = 0.25f;
            //    hudMap.downClip = -0.01f;
            //}
        }
    }



    public bool IsTransitionDone => transitionProgress >= 1;

    public void CopyOrientationFrom(Transform t)
    {
        current = LockedEuler.FromGlobal(t);
    }



    public void BeginTransitionTo(Transform t)
    {
        //Debug.Log($"Begin transition to {t}");
        transitionTarget = t;
        transitioning = true;
        transitionProgress = 0;
    }

    public void AbortTransition()
    {
        if (transitioning)
        {
            current = LockedEuler.FromGlobal(transform);
            //Debug.Log($"Aborting transition. Imported current as {current}");
        }
        transitionTarget = null;
        transitioning = false;
    }



}
