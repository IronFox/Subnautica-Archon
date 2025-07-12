using System;
using UnityEngine;

public class PlayerAdapter : MonoBehaviour
{
    public static Func<GameObject, bool> IsPlayer { get; set; } =
        gameObject => gameObject.GetComponent<FpsTest>() != null;
}
