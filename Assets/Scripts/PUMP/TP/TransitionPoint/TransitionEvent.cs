using System;
using System.Collections.Generic;
using System.Linq;

public class TransitionEventArgs : EventArgs
{
    public List<ITransitionPoint> PassedTP { get; private set; } = new();

    public TransitionEventArgs AddTP(ITransitionPoint tp)
    {
        PassedTP.Add(tp);
        return this;
    }

    public bool HasDuplicates()
    {
        return PassedTP.Count != PassedTP.Distinct().Count();
    }
}

public delegate void StateChangeEventHandler(TransitionEventArgs args);