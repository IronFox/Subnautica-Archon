using UnityEngine;

public class Bioreactor : MonoBehaviour
{
    internal bool isCharging;
    internal bool powerOff;
    public SoundAdapter soundAdapter;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        soundAdapter.volume = powerOff ? 0 : (isCharging ? 1f : 0.2f);
    }
}
