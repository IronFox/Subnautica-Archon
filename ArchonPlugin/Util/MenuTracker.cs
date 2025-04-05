using System;
using UnityEngine;

public class MenuTracker
{
    public MenuTracker(Action onOpen, Action onClose)
    {
        OnOpen = onOpen;
        OnClose = onClose;
    }

    public Action OnOpen { get; }
    public Action OnClose { get; }
    public bool IsOpen { get; private set; }
    public void Update()
    {
        bool isOpen = IngameMenu.main.gameObject.activeSelf && Time.deltaTime == 0;
        if (isOpen != IsOpen)
        {
            if (isOpen)
                OnOpen();
            else
                OnClose();
            IsOpen = isOpen;
        }
    }
}