using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthPickup : Pickup
{
    [SerializeField][Range(1, 1000)] private int regenAmount = 10;

    /// <inheritdoc />
    protected override void OnPickup(GameObject picker) {
        var health = picker.GetComponent<Health>() ?? picker.GetComponentInParent<Health>();
        if (health)
            health.RegenerateHealth(regenAmount);
        else
            Debug.LogWarning("Pickup picker does not contain a Health script: " + picker.name);
    }
}
