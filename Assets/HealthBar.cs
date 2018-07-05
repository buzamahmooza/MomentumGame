using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthBar : MonoBehaviour
{
    float barDisplay = 0;
    [SerializeField]
    Vector2 offset = new Vector2(20, 40),
            size = new Vector2(60, 20);
    [SerializeField]
    Texture2D progressBarEmpty,
    progressBarFull;

    void OnGUI() {

        // draw the background:
        GUI.BeginGroup(new Rect((Vector2)transform.position + offset, (Vector2)transform.position + size));
        GUI.Box(new Rect(0, 0, size.x, size.y), progressBarEmpty);

        // draw the filled-in part:
        GUI.BeginGroup(new Rect(0, 0, size.x * barDisplay, size.y));
        GUI.Box(new Rect(0, 0, size.x, size.y), progressBarFull);
        GUI.EndGroup();

        GUI.EndGroup();

    }

    void Update() {
        // for this example, the bar display is linked to the current time,
        // however you would set this value based on your desired display
        // eg, the loading progress, the player's health, or whatever.
        barDisplay = Time.time * 0.05f;
    }
}
