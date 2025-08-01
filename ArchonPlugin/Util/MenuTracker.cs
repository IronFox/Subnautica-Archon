using AVS.Util;
using UnityEngine;

public class MenuTracker : MonoBehaviour
{
    public delegate void ChangeTrigger();

    public event ChangeTrigger? OnOpen;
    public event ChangeTrigger? OnClose;
    public bool IsOpen { get; private set; }
    public void Update()
    {
        bool isOpen = Character.IsMainMenuOpen && Time.deltaTime == 0;
        if (isOpen != IsOpen)
        {
            if (isOpen)
                OnOpen?.Invoke();
            else
                OnClose?.Invoke();
            IsOpen = isOpen;
        }
    }
}