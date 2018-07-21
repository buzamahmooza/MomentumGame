using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthPickup : Pickup
{
    [SerializeField] [Range(1, 1000)] private int regenAmount = 10;
    [SerializeField] private GameObject floatingHealthText;

    /// <inheritdoc />
    protected override void OnPickup(GameObject picker) {
        var health = picker.GetComponent<Health>() ?? picker.GetComponentInParent<Health>();
        if (health)
            health.RegenerateHealth(regenAmount);
        else
            Debug.LogWarning("Pickup picker does not contain a Health script: " + picker.name);
        if (floatingHealthText) {
            var floatingTextInstance = Instantiate(floatingHealthText, transform.position, Quaternion.identity);
            var floatingText = floatingTextInstance.GetComponent<FloatingText>();
            floatingText.Init(string.Format("+{0}HP", regenAmount));
            floatingText.text.color = Color.red;
            floatingText.text.fontSize -= 3;
        }
    }
}
