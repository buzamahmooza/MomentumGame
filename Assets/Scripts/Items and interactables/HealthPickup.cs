using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthPickup : Pickup
{
    [SerializeField] [Range(1, 1000)] private int regenAmount = 10;
    [SerializeField] private GameObject floatingHealthText;

    /// <inheritdoc />
    protected override void OnPickup(GameObject picker)
    {
        Health health = picker.GetComponent<Health>() ?? picker.GetComponentInParent<Health>();
        if (health)
            health.AddHealth(regenAmount);
        else
            Debug.LogWarning("Pickup picker does not contain a Health script: " + picker.name);
        if (floatingHealthText)
        {
            GameObject floatingTextInstance = Instantiate(floatingHealthText, transform.position, Quaternion.identity);
            FloatingText floatingText = floatingTextInstance.GetComponent<FloatingText>();
            floatingText.Init(string.Format("+{0}HP", regenAmount), Vector3.up*5, false);
            floatingText.text.color = Color.red;
            floatingText.text.fontSize -= 3;
        }
    }
}
