using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public abstract class Pickup : MonoBehaviour
{
    [SerializeField] protected LayerMask layerMask;
    [SerializeField] protected AudioClip audioClip;

    private bool following = false;
    private GameObject picker;
    [SerializeField] private float magnetSmoother = 0.05f;

    private void Awake() {
        //if (layerMask.value != 0)
        //    layerMask = LayerMask.NameToLayer("Player");
        //if (gameObject.layer == 0)
        //    gameObject.layer = LayerMask.NameToLayer("Pickup");
    }

    void LateUpdate() {
        if (following) {
            transform.position = Vector2.Lerp(transform.position, picker.transform.position, magnetSmoother / Vector2.Distance(transform.position, picker.transform.position));
        }
    }

    private void OnTriggerEnter2D(Collider2D col) {
        if (col.gameObject.CompareTag("Pickup picker")) {
            following = true;
            picker = col.gameObject;
            return;
        }

        if ((layerMask.value & 1 << col.gameObject.layer) == 0)
            return;

        // use the GameManager to play the sound since the pickup will be destroyed
        GameManager.AudioSource.PlayOneShot(audioClip);
        picker = col.gameObject;
        OnPickup(picker);

        Destroy(gameObject);
    }

    protected abstract void OnPickup(GameObject picker);
}