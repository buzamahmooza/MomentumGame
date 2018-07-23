using System;
using UnityEngine;

public abstract class Interactable : MonoBehaviour
{

    [SerializeField] private int allowedInteractions = 1;

    public delegate void InteractAction();
    public event InteractAction InteractEvent;

    /// <summary>
    /// This method invokes the InteractEvent and DoInteraction.
    /// Use this method for interacting, do NOT use DoInteraction directly
    /// </summary>
    public void OnInteract() {
        if (InteractEvent != null)
            InteractEvent();

        if (allowedInteractions > 0) {
            allowedInteractions--;
            DoInteraction();
        }
    }
    /// <summary>
    /// Override this method in subclasses, but do not use call, instead use, <see cref="OnInteract"/>
    /// </summary>
    protected abstract void DoInteraction();
    public abstract string GetPrompt();
}