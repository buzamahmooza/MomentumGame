using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public abstract class Pickup : MonoBehaviour
{
    [SerializeField] protected LayerMask layerMask;
    [SerializeField] protected AudioClip audioClip;

    private GameObject picker;
    [SerializeField] private float magnetSmoother = 0.05f;
    private new Collider2D collider2D;
    private bool follow = false;

    private void Awake()
    {
        collider2D = GetComponent<Collider2D>();
        //if (layerMask.value != 0)
        //    layerMask = LayerMask.NameToLayer("Player");
        //if (gameObject.layer == 0)
        //    gameObject.layer = LayerMask.NameToLayer("Pickup");
    }

    // you can't pickup the pickup as soon as it spawns, only a moment after.
    // This is so that pickups spawned form enemies will get the chance to appear and be seen by the player,
    // rather than being sucked in in an instant
    private void Start()
    {
        Invoke("AllowToFollow", 0.5f);
        Destroy(gameObject, 10);
    }
    // ReSharper disable once UnusedMember.Local
    private void AllowToFollow()
    {
        follow = true;
        collider2D.isTrigger = true; // prevent getting stuck through walls
    }

    private void LateUpdate()
    {
        if (follow && picker != null && collider2D.enabled)
        {
            GetComponentInChildren<SpriteRenderer>().color = Color.blue;
            // get closer to the target (Lerp between current position and picker position)
            transform.position = Vector2.Lerp(
                transform.position,
                picker.transform.position,
                magnetSmoother / Mathf.Log(Vector2.Distance(transform.position, picker.transform.position))
            );
        }
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.gameObject.CompareTag("Pickup picker"))
        {
            picker = col.gameObject;
            GetComponentInChildren<SpriteRenderer>().color = Color.green;
            return;
        }

        // if layer is in layerMask
        if ((layerMask.value & 1 << col.gameObject.layer) == 0)
            return;
        if (!picker)
            return;

        // use the GameManager to play the sound since the pickup will be destroyed
        GameComponents.AudioSource.PlayOneShot(audioClip, 0.7f);
        picker = col.gameObject;
        OnPickup(picker);

        Destroy(gameObject);
    }

    protected abstract void OnPickup(GameObject picker);
}