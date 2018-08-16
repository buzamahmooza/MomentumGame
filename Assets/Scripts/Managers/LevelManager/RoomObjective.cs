using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class RoomObjective : MonoBehaviour
{
    public bool IsObjectiveComplete { get; protected set; } = false;

    public Action OnObjectiveComplete;
    public abstract string GetObjectiveMessage();
}