using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Implementations of this interface are notified before and after (off)boarding has completed
/// </summary>
public interface IBoardingListener
{
    void SignalOnboardingBegin();
    void SignalOnboardingEnd();
    void SignalOffBoardingBegin();
    void SignalOffBoardingEnd();
}


public class CommonBoardingListener : MonoBehaviour, IBoardingListener
{
    public virtual void SignalOffBoardingBegin()
    {}

    public virtual void SignalOffBoardingEnd()
    {}

    public virtual void SignalOnboardingBegin()
    {}

    public virtual void SignalOnboardingEnd()
    {}
}

public class BoardingListeners : ListenerSet<IBoardingListener>
{
    

    public BoardingListeners(HashSet<IBoardingListener> listeners):base(listeners)
    {}

    public static BoardingListeners Of(params Component[] origins)
    {
        return Make<BoardingListeners>(origins);
    }

    public void SignalEnterControlBegin()
        => Do(nameof(SignalEnterControlBegin), listener => listener.SignalOnboardingBegin());
    public void SignalEnterControlEnd()
        => Do(nameof(SignalEnterControlEnd), listener => listener.SignalOnboardingEnd());
    public void SignalExitControlBegin()
        => Do(nameof(SignalExitControlBegin), listener => listener.SignalOffBoardingBegin());
    public void SignalExitControlEnd()
        => Do(nameof(SignalExitControlEnd), listener => listener.SignalOffBoardingEnd());
}

