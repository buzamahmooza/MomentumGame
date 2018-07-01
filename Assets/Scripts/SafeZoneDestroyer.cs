using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// (not being used)
/// This class destroys any gameObject that leaves the safe zone, it has one large collider
/// that should be in contact with tha gameObjects at all times, otherwise they're considered to be out of bounds.
/// </summary>
public class SafeZoneDestroyer : MonoBehaviour
{

    private void OnTriggerExit2D(Collider2D collision) {
        Destroy(collision.gameObject);
        print("Object left the safe zone, destroying object: " + collision.gameObject.name);
    }
}
