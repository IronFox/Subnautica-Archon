using UnityEngine;

public class PlayerDetector : MonoBehaviour
{
    public bool hasPlayer;

    public void OnTriggerEnter(Collider other)
    {
        var go = other.GetGameObject();
        if (go && PlayerAdapter.IsPlayer(go))
            hasPlayer = true;
    }
    public void OnTriggerExit(Collider other)
    {
        var go = other.GetGameObject();
        if (go && PlayerAdapter.IsPlayer(go))
            hasPlayer = false;
    }
}
