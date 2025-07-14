using System;
using UnityEngine;

public class PlayerAdapter : MonoBehaviour
{
    public static GameObject PlayerReference { get; set; }
    public static Func<GameObject> Player { get; set; } = () => PlayerReference;
    //public static Func<GameObject, bool> IsPlayer { get; set; } =
    //    gameObject => gameObject.GetComponent<FpsTest>() != null;
}
