using UnityEngine;

namespace Assets.Behavior.Components.Animations
{
    /// <summary>
    /// Controls a single node of a 3-node seat mechanism to match the rotation of a helm seat.
    /// </summary>
    [RequireComponent(typeof(SeatNodeController))]
    public class SeatNodeController : MonoBehaviour
    {

        internal void RotateTo(float angle, Transform seatRoot)
        {
            transform.localEulerAngles = new Vector3(0, angle, 0);
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

}