using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Teleporter : MonoBehaviour
{
    [SerializeField] protected Teleporter otherTeleporter;
    [SerializeField] protected Sprite sprite1, sprite2;
    private AudioSource audioSource;

    // Use this for initialization
    private void Awake()
    {
        //if (otherTeleporter == null) {
        //    otherTeleporter = Instantiate(gameObject).GetComponent<Teleporter>();
        //}

        GetComponent<SpriteRenderer>().sprite = sprite1;
        otherTeleporter.GetComponent<SpriteRenderer>().sprite = sprite2;

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        col.gameObject.transform.position = otherTeleporter.transform.position + Vector3.up;
        audioSource.Play();
        otherTeleporter.GetComponent<Collider2D>().enabled = false;
    }

    private void OnTriggerExit2D(Collider2D col)
    {
        otherTeleporter.GetComponent<Collider2D>().enabled = true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(this.transform.position, otherTeleporter.transform.position);
    }
}
